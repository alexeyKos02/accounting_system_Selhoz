using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    private const string InventoryCheckComment = "Инвентаризация склада";

    /// <summary>
    /// Ведомость инвентаризации по складу (ТЗ §14): активная химия, имеющая остаток на этом складе,
    /// с текущим общим/наливным остатком и количеством упаковок — чтобы сверить с фактом.
    /// </summary>
    public async Task<InventoryCheckSheetDto> GetCheckSheetAsync(Guid warehouseId, CancellationToken ct = default)
    {
        await EnsureWarehouseAsync(warehouseId, ct);
        await RequireWarehouseAccessAsync(warehouseId, ct); // область доступа (ТЗ §6)
        var number = await _db.Warehouses.Where(w => w.Id == warehouseId).Select(w => w.Number).FirstAsync(ct);

        var rows = await _db.ChemicalStockBalances
            .Where(b => b.WarehouseId == warehouseId
                && b.Chemical.ItemType == ItemType.Chemical
                && b.Chemical.Status == ItemStatus.Active)
            .OrderBy(b => b.Chemical.Name)
            .Select(b => new
            {
                b.ChemicalId,
                Name = b.Chemical.Name,
                b.TotalLiters,
                b.LooseLiters,
                FullPackages = _db.PackageGroups
                    .Where(g => g.ChemicalId == b.ChemicalId && g.WarehouseId == warehouseId)
                    .Sum(g => (int?)g.Quantity) ?? 0,
                OpenedPackages = _db.OpenedPackages
                    .Count(o => o.ChemicalId == b.ChemicalId && o.WarehouseId == warehouseId),
            })
            .ToListAsync(ct);

        var lines = rows
            .Select(r => new InventoryCheckLineDto(
                r.ChemicalId, r.Name, r.TotalLiters, r.LooseLiters, r.FullPackages, r.OpenedPackages))
            .ToList();

        return new InventoryCheckSheetDto(warehouseId, number, lines);
    }

    /// <summary>
    /// Применяет фактические остатки инвентаризации (ТЗ §14). Для каждой строки — корректировка
    /// «установить фактический остаток»: разница пишется в движение (Correction) и в audit_log.
    /// Строки без изменений пропускаются; строки, где убыль больше наливного остатка, помечаются
    /// как требующие детальной инвентаризации и не применяются (нужен разбор упаковок, ТЗ §13.2).
    /// </summary>
    public async Task<InventoryCheckResultDto> ApplyCheckAsync(
        InventoryCheckRequest request, CancellationToken ct = default)
    {
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        if (request.Entries.Count == 0)
            throw new ValidationException(nameof(request.Entries), "Ведомость пуста.");

        var ids = request.Entries.Select(e => e.ChemicalId).Distinct().ToList();
        var names = await _db.InventoryItems
            .Where(i => ids.Contains(i.Id) && i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active)
            .ToDictionaryAsync(i => i.Id, i => i.Name, ct);

        var now = _clock.GetUtcNow();
        var occurredAt = request.OccurredAt ?? now;
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? InventoryCheckComment : request.Comment.Trim();

        var results = new List<InventoryCheckLineResult>(request.Entries.Count);

        foreach (var entry in request.Entries)
        {
            if (entry.ActualTotalLiters < 0)
                throw new ValidationException(nameof(entry.ActualTotalLiters),
                    "Фактический остаток не может быть отрицательным.");

            if (!names.TryGetValue(entry.ChemicalId, out var name))
                throw new ConflictException("Инвентаризация доступна только для активной химии.");

            var stock = await LoadStockAsync(entry.ChemicalId, request.WarehouseId, ct, createBalance: true);
            var state = BuildState(stock);
            var prev = state.TotalLiters;
            var delta = entry.ActualTotalLiters - prev;

            if (delta == 0)
            {
                results.Add(new InventoryCheckLineResult(
                    entry.ChemicalId, name, prev, prev, 0, InventoryCheckOutcome.Unchanged));
                continue;
            }

            // Убыль больше наливного остатка нельзя безопасно списать — нужна детальная (ТЗ §13.2).
            if (delta < 0 && -delta > state.LooseLiters)
            {
                results.Add(new InventoryCheckLineResult(
                    entry.ChemicalId, name, prev, prev, 0, InventoryCheckOutcome.NeedsDetailed));
                continue;
            }

            stock.Balance!.LooseLiters += delta;

            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ChemicalId = entry.ChemicalId,
                WarehouseId = request.WarehouseId,
                MovementType = MovementType.Correction,
                QuantityLiters = delta,
                UnitType = UnitType.Liter,
                OccurredAt = occurredAt,
                Comment = comment,
                CreatedByUserId = CurrentUserId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.InventoryMovements.Add(movement);

            Recompute(stock);

            _audit.Log(AuditAction.Update, StockEntityType, stock.Balance.Id,
                new { TotalLiters = prev },
                new { TotalLiters = stock.Balance.TotalLiters, Source = "InventoryCheck", MovementId = movement.Id });

            results.Add(new InventoryCheckLineResult(
                entry.ChemicalId, name, prev, stock.Balance.TotalLiters, delta, InventoryCheckOutcome.Applied));
        }

        await _db.SaveChangesAsync(ct);

        return new InventoryCheckResultDto(
            results.Count(r => r.Outcome == InventoryCheckOutcome.Applied),
            results.Count(r => r.Outcome == InventoryCheckOutcome.Unchanged),
            results.Count(r => r.Outcome == InventoryCheckOutcome.NeedsDetailed),
            results);
    }
}
