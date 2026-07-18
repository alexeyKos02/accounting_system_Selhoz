using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Chemicals;

/// <summary>
/// Химия (ТЗ §15–18): список, карточка, CRUD, архив/восстановление, дубли, объединение.
/// Опасные действия пишутся в audit_log (ТЗ §21).
/// </summary>
public sealed partial class ChemicalService
{
    private const string EntityType = "InventoryItem";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public ChemicalService(IApplicationDbContext db, ICurrentUser currentUser, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _clock = clock;
    }

    // ---------- Чтение ----------

    public async Task<IReadOnlyList<ChemicalListItemDto>> GetListAsync(
        ChemicalListQuery query, CancellationToken ct = default)
    {
        var thresholds = await GetThresholdsAsync(ct);

        var q = _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(i => i.Name.ToLower().Contains(s));
        }

        if (query.CropId is { } cropId)
            q = q.Where(i => i.ChemicalCrops.Any(cc => cc.CropId == cropId));

        if (query.WarehouseId is { } warehouseId)
            q = q.Where(i => _db.ChemicalStockBalances.Any(b => b.ChemicalId == i.Id && b.WarehouseId == warehouseId));

        if (query.Type is { } type)
            q = q.Where(i => i.ChemicalDetails!.Type == type);

        // MVP: сортировка только по названию (ТЗ §16.3), направление учитывается.
        q = query.SortDirection == SortDirection.Desc
            ? q.OrderByDescending(i => i.Name)
            : q.OrderBy(i => i.Name);

        var rows = await q
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.MeasureUnit,
                Type = i.ChemicalDetails!.Type,
                Crops = i.ChemicalCrops.Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name)).ToList(),
                Total = _db.ChemicalStockBalances
                    .Where(b => b.ChemicalId == i.Id)
                    .Sum(b => (decimal?)b.TotalQuantity) ?? 0m,
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ChemicalListItemDto(
                r.Id, r.Name, r.Type, r.MeasureUnit, r.Total, r.Crops, Badge(r.Total, thresholds.For(r.MeasureUnit))))
            .ToList();
    }

    public async Task<ChemicalDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.ChemicalDetails)
            .Include(i => i.ChemicalCrops).ThenInclude(cc => cc.Crop)
            .Include(i => i.CanonicalChemical)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw NotFoundException.For("Химия", id);

        var crops = item.ChemicalCrops
            .Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name))
            .OrderBy(c => c.Name)
            .ToList();

        var warehouses = await BuildWarehouseStocksAsync(id, ct);
        var total = warehouses.Sum(w => w.TotalQuantity);

        return new ChemicalDetailDto(
            item.Id, item.Name, item.ChemicalDetails?.Type, item.MeasureUnit,
            item.ChemicalDetails?.Manufacturer, item.ChemicalDetails?.Comment,
            item.Status, item.MergedIntoItemId,
            item.CanonicalChemicalId, item.CanonicalChemical?.CanonicalName,
            crops, total, warehouses);
    }

    public async Task<IReadOnlyList<ArchivedChemicalDto>> GetArchivedAsync(CancellationToken ct = default)
    {
        var items = await _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Archived)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.MeasureUnit,
                i.UpdatedAt,
                Type = i.ChemicalDetails!.Type,
                Manufacturer = i.ChemicalDetails!.Manufacturer,
                Crops = i.ChemicalCrops.Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name)).ToList(),
                Total = _db.ChemicalStockBalances.Where(b => b.ChemicalId == i.Id).Sum(b => (decimal?)b.TotalQuantity) ?? 0m,
            })
            .ToListAsync(ct);

        var ids = items.Select(i => i.Id).ToList();
        var archivedAt = await _db.AuditLogs
            .Where(a => a.EntityType == EntityType && a.Action == AuditAction.Archive && ids.Contains(a.EntityId))
            .GroupBy(a => a.EntityId)
            .Select(g => new { Id = g.Key, At = g.Max(x => x.CreatedAt) })
            .ToDictionaryAsync(x => x.Id, x => x.At, ct);

        return items
            .Select(i => new ArchivedChemicalDto(
                i.Id, i.Name, i.Type, i.MeasureUnit, i.Manufacturer, i.Crops, i.Total,
                archivedAt.TryGetValue(i.Id, out var at) ? at : i.UpdatedAt))
            .ToList();
    }

    /// <summary>Поиск похожих карточек для предупреждения о дублях (ТЗ §18.1).</summary>
    public async Task<IReadOnlyList<DuplicateDto>> FindDuplicatesAsync(string name, CancellationToken ct = default)
    {
        var query = (name ?? string.Empty).Trim();
        if (query.Length == 0) return Array.Empty<DuplicateDto>();

        // Небольшой справочник — сравниваем в памяти (lower-case + похожесть строк).
        var candidates = await _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active)
            .Select(i => new DuplicateDto(i.Id, i.Name, i.ChemicalDetails!.Manufacturer))
            .ToListAsync(ct);

        return candidates
            .Where(c => StringSimilarity.AreSimilar(c.Name, query))
            .OrderBy(c => c.Name)
            .ToList();
    }

    // ---------- Вспомогательное ----------

    private async Task<Thresholds> GetThresholdsAsync(CancellationToken ct)
    {
        var s = await _db.AppSettings
            .Select(s => new { s.LowStockThresholdLiters, s.LowStockThresholdKg })
            .FirstOrDefaultAsync(ct);
        return new Thresholds(s?.LowStockThresholdLiters ?? 0m, s?.LowStockThresholdKg ?? 0m);
    }

    private static StockStatus Badge(decimal total, decimal threshold) =>
        total <= 0 ? StockStatus.Empty
        : total < threshold ? StockStatus.Low
        : StockStatus.InStock;

    private async Task<List<WarehouseStockDto>> BuildWarehouseStocksAsync(Guid chemicalId, CancellationToken ct)
    {
        return await _db.ChemicalStockBalances
            .Where(b => b.ChemicalId == chemicalId)
            .OrderBy(b => b.Warehouse.Number)
            .Select(b => new WarehouseStockDto(b.WarehouseId, b.Warehouse.Number, b.TotalQuantity))
            .ToListAsync(ct);
    }

    /// <summary>Пороги малого остатка по единицам измерения.</summary>
    private readonly record struct Thresholds(decimal Liters, decimal Kg)
    {
        public decimal For(MeasureUnit unit) => unit == MeasureUnit.Kilogram ? Kg : Liters;
    }
}
