using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

/// <summary>
/// Складские операции (ТЗ §9–13): приход, списание, корректировка. Источник истины —
/// inventory_movements; быстрый баланс пересчитывается после каждой операции (ТЗ §31).
/// </summary>
public sealed partial class InventoryService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public InventoryService(IApplicationDbContext db, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    // ---------- Приход (ТЗ §10) ----------

    public async Task<IncomeResultDto> IncomeAsync(IncomeRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException(nameof(request.Quantity), "Количество должно быть больше нуля.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);

        var stock = await LoadStockAsync(request.ChemicalId, request.WarehouseId, ct, createBalance: true);
        var now = _clock.GetUtcNow();
        var occurredAt = request.OccurredAt ?? now;

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ChemicalId = request.ChemicalId,
            WarehouseId = request.WarehouseId,
            MovementType = MovementType.Income,
            CropId = null,
            OccurredAt = occurredAt,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedByUserId = SystemUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (request.Unit == UnitType.Liter)
        {
            stock.Balance!.LooseLiters += request.Quantity;
            movement.QuantityLiters = request.Quantity;
            movement.UnitType = UnitType.Liter;
        }
        else
        {
            if (request.PackageVolumeLiters is not { } volume || volume <= 0)
                throw new ValidationException(nameof(request.PackageVolumeLiters),
                    "Для банок/штук укажите литраж упаковки.");
            if (request.Quantity % 1 != 0)
                throw new ValidationException(nameof(request.Quantity), "Количество упаковок должно быть целым.");

            var count = (int)request.Quantity;
            var group = stock.Groups.FirstOrDefault(g => g.UnitType == request.Unit && g.PackageVolumeLiters == volume);
            if (group is null)
            {
                group = new PackageGroup
                {
                    Id = Guid.NewGuid(),
                    CompanyId = CompanyId,
                    ChemicalId = request.ChemicalId,
                    WarehouseId = request.WarehouseId,
                    UnitType = request.Unit,
                    PackageVolumeLiters = volume,
                    Quantity = 0,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _db.PackageGroups.Add(group);
                stock.Groups.Add(group);
            }

            group.Quantity += count;
            group.UpdatedAt = now;

            movement.QuantityLiters = count * volume;
            movement.UnitType = request.Unit;
            movement.PackageVolumeLiters = volume;
            movement.PackagesQuantity = count;
        }

        _db.InventoryMovements.Add(movement);
        Recompute(stock);
        await _db.SaveChangesAsync(ct);

        return new IncomeResultDto(stock.Balance!.TotalLiters, movement.Id);
    }

    // ---------- Пересчёт баланса из текущих остатков (ТЗ §31.3) ----------

    /// <summary>Пересчитывает быстрый баланс из активных остатков (loose + полные + вскрытые).</summary>
    public async Task RecalculateBalanceAsync(Guid chemicalId, Guid warehouseId, CancellationToken ct = default)
    {
        var stock = await LoadStockAsync(chemicalId, warehouseId, ct, createBalance: true);
        Recompute(stock);
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Общие помощники ----------

    private Guid SystemUserId => Domain.Constants.SystemIds.SystemUserId;

    /// <summary>
    /// Хозяйство-контекст для штампа company_id на новых записях (ТЗ §13, §25).
    /// До авторизации — дефолтное хозяйство; на этапе C заменяется на выбранное пользователем.
    /// </summary>
    private Guid CompanyId => Domain.Constants.SystemIds.DefaultCompanyId;

    private async Task EnsureActiveChemicalAsync(Guid id, CancellationToken ct)
    {
        var status = await _db.InventoryItems
            .Where(i => i.Id == id && i.ItemType == ItemType.Chemical)
            .Select(i => (ItemStatus?)i.Status)
            .FirstOrDefaultAsync(ct);

        if (status is null) throw NotFoundException.For("Химия", id);
        if (status != ItemStatus.Active)
            throw new ConflictException("Операция доступна только для активной химии.");
    }

    private async Task EnsureWarehouseAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Warehouses.AnyAsync(w => w.Id == id, ct))
            throw NotFoundException.For("Склад", id);
    }

    private async Task EnsureCropAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Crops.AnyAsync(c => c.Id == id, ct))
            throw NotFoundException.For("Культура", id);
    }

    private async Task EnsureFieldAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Fields.AnyAsync(f => f.Id == id, ct))
            throw NotFoundException.For("Поле", id);
    }

    private async Task<LoadedStock> LoadStockAsync(
        Guid chemicalId, Guid warehouseId, CancellationToken ct, bool createBalance = false)
    {
        var balance = await _db.ChemicalStockBalances
            .FirstOrDefaultAsync(b => b.ChemicalId == chemicalId && b.WarehouseId == warehouseId, ct);

        if (balance is null && createBalance)
        {
            balance = new ChemicalStockBalance
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ChemicalId = chemicalId,
                WarehouseId = warehouseId,
                LooseLiters = 0,
                TotalLiters = 0,
                UpdatedAt = _clock.GetUtcNow(),
            };
            _db.ChemicalStockBalances.Add(balance);
        }

        var groups = await _db.PackageGroups
            .Where(g => g.ChemicalId == chemicalId && g.WarehouseId == warehouseId).ToListAsync(ct);
        var opened = await _db.OpenedPackages
            .Where(o => o.ChemicalId == chemicalId && o.WarehouseId == warehouseId).ToListAsync(ct);

        return new LoadedStock(balance, groups, opened);
    }

    private static StockState BuildState(LoadedStock stock) =>
        new(
            stock.Balance?.LooseLiters ?? 0,
            stock.Groups.Select(g => new GroupState(g.Id, g.UnitType, g.PackageVolumeLiters, g.Quantity)).ToList(),
            stock.Opened.Select(o => new OpenedState(o.Id, o.UnitType, o.InitialLiters, o.RemainingLiters)).ToList());

    /// <summary>Пересчитывает total_liters и убирает пустые упаковки (ТЗ §8.4, §11.5).</summary>
    private void Recompute(LoadedStock stock)
    {
        var now = _clock.GetUtcNow();

        // Пустые вскрытые упаковки не храним (ТЗ §8.4).
        foreach (var empty in stock.Opened.Where(o => o.RemainingLiters <= 0).ToList())
        {
            _db.OpenedPackages.Remove(empty);
            stock.Opened.Remove(empty);
        }

        // Пустые группы полных упаковок тоже убираем.
        foreach (var empty in stock.Groups.Where(g => g.Quantity <= 0).ToList())
        {
            _db.PackageGroups.Remove(empty);
            stock.Groups.Remove(empty);
        }

        if (stock.Balance is null) return;
        stock.Balance.TotalLiters =
            stock.Balance.LooseLiters
            + stock.Groups.Sum(g => g.PackageVolumeLiters * g.Quantity)
            + stock.Opened.Sum(o => o.RemainingLiters);
        stock.Balance.UpdatedAt = now;
    }

    private sealed class LoadedStock
    {
        public LoadedStock(ChemicalStockBalance? balance, List<PackageGroup> groups, List<OpenedPackage> opened)
        {
            Balance = balance;
            Groups = groups;
            Opened = opened;
        }

        public ChemicalStockBalance? Balance { get; }
        public List<PackageGroup> Groups { get; }
        public List<OpenedPackage> Opened { get; }
    }
}
