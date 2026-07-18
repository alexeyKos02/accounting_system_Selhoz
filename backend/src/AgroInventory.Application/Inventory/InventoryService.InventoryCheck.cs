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
    /// с текущим остатком в единице химии — чтобы сверить с фактом.
    /// </summary>
    public async Task<InventoryCheckSheetDto> GetCheckSheetAsync(Guid warehouseId, CancellationToken ct = default)
    {
        await EnsureWarehouseAsync(warehouseId, ct);
        await RequireWarehouseAccessAsync(warehouseId, ct); // область доступа (ТЗ §6)
        var number = await _db.Warehouses.Where(w => w.Id == warehouseId).Select(w => w.Number).FirstAsync(ct);

        var lines = await _db.ChemicalStockBalances
            .Where(b => b.WarehouseId == warehouseId
                && b.Chemical.ItemType == ItemType.Chemical
                && b.Chemical.Status == ItemStatus.Active)
            .OrderBy(b => b.Chemical.Name)
            .Select(b => new InventoryCheckLineDto(
                b.ChemicalId, b.Chemical.Name, b.Chemical.MeasureUnit, b.TotalQuantity))
            .ToListAsync(ct);

        return new InventoryCheckSheetDto(warehouseId, number, lines);
    }

    /// <summary>
    /// Применяет фактические остатки инвентаризации (ТЗ §14). Для каждой строки — корректировка
    /// «установить фактический остаток»: разница пишется в движение (Correction) и в audit_log.
    /// Строки без изменений пропускаются.
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
            if (entry.ActualTotalQuantity < 0)
                throw new ValidationException(nameof(entry.ActualTotalQuantity),
                    "Фактический остаток не может быть отрицательным.");

            if (!names.TryGetValue(entry.ChemicalId, out var name))
                throw new ConflictException("Инвентаризация доступна только для активной химии.");

            var balance = await LoadBalanceAsync(entry.ChemicalId, request.WarehouseId, ct, createBalance: true);
            var prev = balance!.TotalQuantity;
            var delta = entry.ActualTotalQuantity - prev;

            if (delta == 0)
            {
                results.Add(new InventoryCheckLineResult(
                    entry.ChemicalId, name, prev, prev, 0, InventoryCheckOutcome.Unchanged));
                continue;
            }

            balance.TotalQuantity = entry.ActualTotalQuantity;
            balance.UpdatedAt = now;

            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ChemicalId = entry.ChemicalId,
                WarehouseId = request.WarehouseId,
                MovementType = MovementType.Correction,
                Quantity = delta,
                OccurredAt = occurredAt,
                Comment = comment,
                CreatedByUserId = CurrentUserId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.InventoryMovements.Add(movement);

            _audit.Log(AuditAction.Update, StockEntityType, balance.Id,
                new { TotalQuantity = prev },
                new { TotalQuantity = balance.TotalQuantity, Source = "InventoryCheck", MovementId = movement.Id });

            results.Add(new InventoryCheckLineResult(
                entry.ChemicalId, name, prev, balance.TotalQuantity, delta, InventoryCheckOutcome.Applied));
        }

        await _db.SaveChangesAsync(ct);

        return new InventoryCheckResultDto(
            results.Count(r => r.Outcome == InventoryCheckOutcome.Applied),
            results.Count(r => r.Outcome == InventoryCheckOutcome.Unchanged),
            results);
    }
}
