using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Catalog;

/// <summary>
/// Общий режим «Все хозяйства» для химии (ТЗ §17): остатки по всем доступным пользователю хозяйствам,
/// сгруппированные по каноническому препарату. Изоляция сохраняется — берём только доступные хозяйства
/// и склады (область доступа). Объединяем только через общий каталог, а не по названию (ТЗ §12).
/// </summary>
public sealed class AggregatedChemicalService
{
    private readonly IApplicationDbContext _db;
    private readonly CompanyContextService _companyContext;

    public AggregatedChemicalService(IApplicationDbContext db, CompanyContextService companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    public async Task<IReadOnlyList<AggregatedChemicalGroupDto>> GetAsync(CancellationToken ct = default)
    {
        var accessible = await _companyContext.GetAccessibleCompaniesAsync(ct);
        if (accessible.Count == 0) return Array.Empty<AggregatedChemicalGroupDto>();

        var byCompany = accessible.ToDictionary(a => a.CompanyId);
        var companyIds = accessible.Select(a => a.CompanyId).ToList();

        // IgnoreQueryFilters: осознанно читаем несколько хозяйств; ограничение — только доступными
        // хозяйствами (и складами ниже), а не выбранным в заголовке (ТЗ §17, §24).
        var rows = await _db.ChemicalStockBalances.IgnoreQueryFilters()
            .Where(b => companyIds.Contains(b.CompanyId)
                        && b.Chemical.ItemType == ItemType.Chemical
                        && b.Chemical.Status == ItemStatus.Active
                        && b.TotalLiters > 0)
            .Select(b => new Row(
                b.CompanyId,
                b.ChemicalId,
                b.Chemical.Name,
                b.Chemical.CanonicalChemicalId,
                b.Chemical.CanonicalChemical!.CanonicalName,
                b.WarehouseId,
                b.Warehouse.Number,
                b.TotalLiters))
            .ToListAsync(ct);

        // Фильтр по области доступа к складам (ТЗ §6): при неполном scope — только разрешённые склады.
        rows = rows.Where(r =>
        {
            var acc = byCompany[r.CompanyId];
            return acc.HasFullScope || acc.WarehouseIds.Contains(r.WarehouseId);
        }).ToList();

        // Группа: привязанные к канону — по CanonicalChemicalId; непривязанные — каждая карточка отдельно.
        var groups = rows
            .GroupBy(r => r.CanonicalChemicalId is { } cid ? $"canon:{cid}" : $"item:{r.ChemicalId}")
            .Select(g =>
            {
                var first = g.First();
                var linked = first.CanonicalChemicalId is not null;
                var name = linked ? first.CanonicalName! : first.LocalName;

                // Позиции внутри группы — по карточке хозяйства (InventoryItemId).
                var positions = g
                    .GroupBy(r => r.ChemicalId)
                    .Select(pg =>
                    {
                        var p = pg.First();
                        var warehouses = pg
                            .GroupBy(r => new { r.WarehouseId, r.WarehouseNumber })
                            .Select(wg => new AggregatedWarehouseDto(
                                wg.Key.WarehouseId, wg.Key.WarehouseNumber, wg.Sum(x => x.TotalLiters)))
                            .OrderBy(w => w.WarehouseNumber)
                            .ToList();
                        return new AggregatedPositionDto(
                            p.CompanyId, byCompany[p.CompanyId].Name, p.ChemicalId, p.LocalName,
                            pg.Sum(x => x.TotalLiters), warehouses);
                    })
                    .OrderBy(p => p.CompanyName)
                    .ToList();

                return new AggregatedChemicalGroupDto(
                    g.Key,
                    name,
                    linked,
                    linked ? first.CanonicalChemicalId : null,
                    positions.Sum(p => p.TotalLiters),
                    positions.Select(p => p.CompanyId).Distinct().Count(),
                    g.Select(r => r.WarehouseId).Distinct().Count(),
                    positions);
            })
            .OrderBy(x => x.Name)
            .ToList();

        return groups;
    }

    private sealed record Row(
        Guid CompanyId, Guid ChemicalId, string LocalName, Guid? CanonicalChemicalId, string? CanonicalName,
        Guid WarehouseId, string WarehouseNumber, decimal TotalLiters);
}
