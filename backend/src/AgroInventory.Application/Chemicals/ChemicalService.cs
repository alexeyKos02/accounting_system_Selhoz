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
        var threshold = await GetLowStockThresholdAsync(ct);

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
                Type = i.ChemicalDetails!.Type,
                Crops = i.ChemicalCrops.Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name)).ToList(),
                Total = _db.ChemicalStockBalances
                    .Where(b => b.ChemicalId == i.Id)
                    .Sum(b => (decimal?)b.TotalLiters) ?? 0m,
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ChemicalListItemDto(r.Id, r.Name, r.Type, r.Total, r.Crops, Badge(r.Total, threshold)))
            .ToList();
    }

    public async Task<ChemicalDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.ChemicalDetails)
            .Include(i => i.ChemicalCrops).ThenInclude(cc => cc.Crop)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw NotFoundException.For("Химия", id);

        var crops = item.ChemicalCrops
            .Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name))
            .OrderBy(c => c.Name)
            .ToList();

        var warehouses = await BuildWarehouseStocksAsync(id, ct);
        var total = warehouses.Sum(w => w.TotalLiters);

        return new ChemicalDetailDto(
            item.Id, item.Name, item.ChemicalDetails?.Type, item.ChemicalDetails?.Manufacturer, item.ChemicalDetails?.Comment,
            item.Status, item.MergedIntoItemId, crops, total, warehouses);
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
                i.UpdatedAt,
                Type = i.ChemicalDetails!.Type,
                Manufacturer = i.ChemicalDetails!.Manufacturer,
                Crops = i.ChemicalCrops.Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name)).ToList(),
                Total = _db.ChemicalStockBalances.Where(b => b.ChemicalId == i.Id).Sum(b => (decimal?)b.TotalLiters) ?? 0m,
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
                i.Id, i.Name, i.Type, i.Manufacturer, i.Crops, i.Total,
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

    private async Task<decimal> GetLowStockThresholdAsync(CancellationToken ct) =>
        await _db.AppSettings.Select(s => s.LowStockThresholdLiters).FirstOrDefaultAsync(ct);

    private static StockStatus Badge(decimal total, decimal threshold) =>
        total <= 0 ? StockStatus.Empty
        : total < threshold ? StockStatus.Low
        : StockStatus.InStock;

    private async Task<List<WarehouseStockDto>> BuildWarehouseStocksAsync(Guid chemicalId, CancellationToken ct)
    {
        var balances = await _db.ChemicalStockBalances
            .Where(b => b.ChemicalId == chemicalId)
            .Select(b => new { b.WarehouseId, b.Warehouse.Number, b.LooseLiters, b.TotalLiters })
            .ToListAsync(ct);

        var groups = await _db.PackageGroups
            .Where(g => g.ChemicalId == chemicalId)
            .Select(g => new { g.WarehouseId, Dto = new PackageGroupDto(g.Id, g.UnitType, g.PackageVolumeLiters, g.Quantity) })
            .ToListAsync(ct);

        var opened = await _db.OpenedPackages
            .Where(o => o.ChemicalId == chemicalId)
            .Select(o => new { o.WarehouseId, Dto = new OpenedPackageDto(o.Id, o.UnitType, o.InitialLiters, o.RemainingLiters) })
            .ToListAsync(ct);

        // Номера складов для тех, у кого есть упаковки, но нет строки баланса (страховка).
        var extraWarehouseIds = groups.Select(g => g.WarehouseId)
            .Concat(opened.Select(o => o.WarehouseId))
            .Where(wid => balances.All(b => b.WarehouseId != wid))
            .Distinct()
            .ToList();

        var extraNumbers = extraWarehouseIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Warehouses.Where(w => extraWarehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Number, ct);

        var warehouseIds = balances.Select(b => b.WarehouseId)
            .Concat(extraWarehouseIds)
            .Distinct()
            .ToList();

        var result = new List<WarehouseStockDto>();
        foreach (var wid in warehouseIds)
        {
            var balance = balances.FirstOrDefault(b => b.WarehouseId == wid);
            var number = balance?.Number ?? (extraNumbers.TryGetValue(wid, out var n) ? n : "?");
            var wGroups = groups.Where(g => g.WarehouseId == wid).Select(g => g.Dto).ToList();
            var wOpened = opened.Where(o => o.WarehouseId == wid).Select(o => o.Dto).ToList();

            var loose = balance?.LooseLiters ?? 0m;
            var total = balance?.TotalLiters
                        ?? loose
                        + wGroups.Sum(g => g.PackageVolumeLiters * g.Quantity)
                        + wOpened.Sum(o => o.RemainingLiters);

            result.Add(new WarehouseStockDto(wid, number, total, loose, wGroups, wOpened));
        }

        return result.OrderBy(w => w.WarehouseNumber).ToList();
    }
}
