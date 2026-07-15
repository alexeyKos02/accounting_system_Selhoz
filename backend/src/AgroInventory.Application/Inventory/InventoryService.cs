using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Constants;
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
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public InventoryService(
        IApplicationDbContext db, ICurrentUser currentUser,
        CompanyContextService companyContext, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
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
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        var stock = await LoadStockAsync(request.ChemicalId, request.WarehouseId, ct, createBalance: true);
        var now = _clock.GetUtcNow();
        var occurredAt = request.OccurredAt ?? now;

        var movement = ApplyIncome(
            stock,
            CompanyId,
            request.ChemicalId,
            request.WarehouseId,
            request.Unit,
            request.Quantity,
            request.PackageVolumeLiters,
            occurredAt,
            request.Comment,
            now);
        Recompute(stock);
        await _db.SaveChangesAsync(ct);

        return new IncomeResultDto(stock.Balance!.TotalLiters, movement.Id);
    }

    public async Task<BulkIncomeOptionsDto> GetBulkIncomeOptionsAsync(
        Guid canonicalChemicalId, CancellationToken ct = default)
    {
        if (!await _db.CanonicalChemicals.AnyAsync(c => c.Id == canonicalChemicalId, ct))
            throw NotFoundException.For("Препарат общего справочника", canonicalChemicalId);

        var accessible = await _companyContext.GetAccessibleCompaniesAsync(ct);
        if (accessible.Count == 0) return new BulkIncomeOptionsDto(Array.Empty<BulkIncomeCompanyOptionDto>());

        var companyIds = accessible.Select(c => c.CompanyId).ToList();
        var companyAccess = new Dictionary<Guid, CompanyAccess>();
        foreach (var company in accessible)
            companyAccess[company.CompanyId] = await _companyContext.RequireForCompanyAsync(company.CompanyId, ct);

        var chemicals = await _db.InventoryItems.IgnoreQueryFilters()
            .Where(i => companyIds.Contains(i.CompanyId)
                        && i.ItemType == ItemType.Chemical
                        && i.Status == ItemStatus.Active
                        && i.CanonicalChemicalId == canonicalChemicalId)
            .Select(i => new { i.CompanyId, i.Id, i.Name })
            .ToListAsync(ct);

        var warehouses = await _db.Warehouses.IgnoreQueryFilters()
            .Where(w => companyIds.Contains(w.CompanyId))
            .Select(w => new { w.CompanyId, w.Id, w.Number })
            .ToListAsync(ct);

        var result = accessible
            .OrderBy(c => c.Name)
            .Select(company =>
            {
                var access = companyAccess[company.CompanyId];
                var chemical = chemicals
                    .Where(i => i.CompanyId == company.CompanyId)
                    .OrderBy(i => i.Name)
                    .FirstOrDefault();
                var allowedWarehouses = warehouses
                    .Where(w => w.CompanyId == company.CompanyId && access.CanAccessWarehouse(w.Id))
                    .OrderBy(w => w.Number)
                    .Select(w => new BulkIncomeWarehouseOptionDto(w.Id, w.Number))
                    .ToList();

                string? blockedReason = null;
                if (!access.Has(Permissions.ReceiptsCreate))
                    blockedReason = "Нет права на создание прихода в этом хозяйстве.";
                else if (chemical is null)
                    blockedReason = "Нет связанной карточки химии в этом хозяйстве.";
                else if (allowedWarehouses.Count == 0)
                    blockedReason = "Нет доступных складов в этом хозяйстве.";

                return new BulkIncomeCompanyOptionDto(
                    company.CompanyId,
                    company.Name,
                    chemical?.Id,
                    chemical?.Name,
                    allowedWarehouses,
                    blockedReason);
            })
            .ToList();

        return new BulkIncomeOptionsDto(result);
    }

    public async Task<BulkIncomeResultDto> BulkIncomeAsync(
        BulkIncomeRequest request, CancellationToken ct = default)
    {
        if (request.Lines.Count == 0)
            throw new ValidationException(nameof(request.Lines), "Добавьте хотя бы одно хозяйство.");
        if (request.Lines.Any(l => l.Quantity <= 0))
            throw new ValidationException(nameof(request.Lines), "Количество должно быть больше нуля.");
        if (request.Unit != UnitType.Liter)
        {
            if (request.PackageVolumeLiters is not { } volume || volume <= 0)
                throw new ValidationException(nameof(request.PackageVolumeLiters),
                    "Для банок/штук укажите литраж упаковки.");
            if (request.Lines.Any(l => l.Quantity % 1 != 0))
                throw new ValidationException(nameof(request.Lines), "Количество упаковок должно быть целым.");
        }

        if (!await _db.CanonicalChemicals.AnyAsync(c => c.Id == request.CanonicalChemicalId, ct))
            throw NotFoundException.For("Препарат общего справочника", request.CanonicalChemicalId);

        var duplicateCompany = request.Lines
            .GroupBy(l => l.CompanyId)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateCompany is not null)
            throw new ValidationException(nameof(request.Lines),
                "Одно хозяйство можно указать только один раз.");

        var companyIds = request.Lines.Select(l => l.CompanyId).ToList();
        var warehouseIds = request.Lines.Select(l => l.WarehouseId).ToList();

        var accessByCompany = new Dictionary<Guid, CompanyAccess>();
        foreach (var companyId in companyIds)
        {
            var access = await _companyContext.RequireForCompanyAsync(companyId, ct);
            access.Require(Permissions.ReceiptsCreate);
            accessByCompany[companyId] = access;
        }

        var warehouses = await _db.Warehouses.IgnoreQueryFilters()
            .Where(w => warehouseIds.Contains(w.Id))
            .Select(w => new { w.Id, w.CompanyId, w.Number })
            .ToListAsync(ct);

        var chemicals = await _db.InventoryItems.IgnoreQueryFilters()
            .Where(i => companyIds.Contains(i.CompanyId)
                        && i.ItemType == ItemType.Chemical
                        && i.Status == ItemStatus.Active
                        && i.CanonicalChemicalId == request.CanonicalChemicalId)
            .Select(i => new { i.CompanyId, i.Id, i.Name })
            .ToListAsync(ct);

        foreach (var line in request.Lines)
        {
            var warehouse = warehouses.FirstOrDefault(w => w.Id == line.WarehouseId);
            if (warehouse is null || warehouse.CompanyId != line.CompanyId)
                throw new ValidationException(nameof(line.WarehouseId),
                    "Склад не найден в выбранном хозяйстве.");
            accessByCompany[line.CompanyId].RequireWarehouse(line.WarehouseId);

            if (!chemicals.Any(i => i.CompanyId == line.CompanyId))
                throw new ConflictException(
                    "В одном из выбранных хозяйств нет локальной карточки химии, связанной с общим препаратом. Приход не создан.");
        }

        var now = _clock.GetUtcNow();
        var occurredAt = request.OccurredAt ?? now;
        var results = new List<BulkIncomeLineResultDto>();

        foreach (var line in request.Lines)
        {
            var chemical = chemicals
                .Where(i => i.CompanyId == line.CompanyId)
                .OrderBy(i => i.Name)
                .First();
            var stock = await LoadStockAsync(
                chemical.Id,
                line.WarehouseId,
                ct,
                createBalance: true,
                companyId: line.CompanyId);
            var movement = ApplyIncome(
                stock,
                line.CompanyId,
                chemical.Id,
                line.WarehouseId,
                request.Unit,
                line.Quantity,
                request.PackageVolumeLiters,
                occurredAt,
                request.Comment,
                now);
            Recompute(stock);
            results.Add(new BulkIncomeLineResultDto(
                line.CompanyId, chemical.Id, line.WarehouseId, stock.Balance!.TotalLiters, movement.Id));
        }

        await _db.SaveChangesAsync(ct);
        return new BulkIncomeResultDto(results);
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

    private Guid CurrentUserId => _currentUser.UserId;

    /// <summary>Хозяйство-контекст для штампа company_id (выбранное хозяйство, ТЗ §13, §24).</summary>
    private Guid CompanyId => _currentUser.CompanyId;

    /// <summary>Проверяет доступ к складу в рамках области доступа членства (ТЗ §6).</summary>
    private async Task RequireWarehouseAccessAsync(Guid warehouseId, CancellationToken ct)
    {
        var access = await _companyContext.RequireAsync(ct);
        access.RequireWarehouse(warehouseId);
    }

    /// <summary>Проверяет доступ к полю в рамках области доступа членства (ТЗ §6).</summary>
    private async Task RequireFieldAccessAsync(Guid fieldId, CancellationToken ct)
    {
        var access = await _companyContext.RequireAsync(ct);
        access.RequireField(fieldId);
    }

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

    private InventoryMovement ApplyIncome(
        LoadedStock stock,
        Guid companyId,
        Guid chemicalId,
        Guid warehouseId,
        UnitType unit,
        decimal quantity,
        decimal? packageVolumeLiters,
        DateTimeOffset occurredAt,
        string? comment,
        DateTimeOffset now)
    {
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ChemicalId = chemicalId,
            WarehouseId = warehouseId,
            MovementType = MovementType.Income,
            CropId = null,
            OccurredAt = occurredAt,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedByUserId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (unit == UnitType.Liter)
        {
            stock.Balance!.LooseLiters += quantity;
            movement.QuantityLiters = quantity;
            movement.UnitType = UnitType.Liter;
        }
        else
        {
            var volume = packageVolumeLiters!.Value;
            var count = (int)quantity;
            var group = stock.Groups.FirstOrDefault(g => g.UnitType == unit && g.PackageVolumeLiters == volume);
            if (group is null)
            {
                group = new PackageGroup
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    ChemicalId = chemicalId,
                    WarehouseId = warehouseId,
                    UnitType = unit,
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
            movement.UnitType = unit;
            movement.PackageVolumeLiters = volume;
            movement.PackagesQuantity = count;
        }

        _db.InventoryMovements.Add(movement);
        return movement;
    }

    private async Task<LoadedStock> LoadStockAsync(
        Guid chemicalId,
        Guid warehouseId,
        CancellationToken ct,
        bool createBalance = false,
        Guid? companyId = null)
    {
        var balances = companyId is null
            ? _db.ChemicalStockBalances
            : _db.ChemicalStockBalances.IgnoreQueryFilters();
        var balance = await balances
            .FirstOrDefaultAsync(b => b.ChemicalId == chemicalId && b.WarehouseId == warehouseId, ct);

        if (balance is null && createBalance)
        {
            balance = new ChemicalStockBalance
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId ?? CompanyId,
                ChemicalId = chemicalId,
                WarehouseId = warehouseId,
                LooseLiters = 0,
                TotalLiters = 0,
                UpdatedAt = _clock.GetUtcNow(),
            };
            _db.ChemicalStockBalances.Add(balance);
        }

        var packageGroups = companyId is null ? _db.PackageGroups : _db.PackageGroups.IgnoreQueryFilters();
        var groups = await packageGroups
            .Where(g => g.ChemicalId == chemicalId && g.WarehouseId == warehouseId).ToListAsync(ct);
        var openedPackages = companyId is null ? _db.OpenedPackages : _db.OpenedPackages.IgnoreQueryFilters();
        var opened = await openedPackages
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
