using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Chemicals;
using AgroInventory.Application.History;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Dashboard;

/// <summary>Сводка для главного экрана (ТЗ §22): что закончилось, что на исходе, последние операции.</summary>
public sealed class DashboardService
{
    private const int RecentOperationsLimit = 10;

    private readonly IApplicationDbContext _db;
    private readonly CompanyContextService _companyContext;

    public DashboardService(IApplicationDbContext db, CompanyContextService companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var threshold = await _db.AppSettings.Select(s => s.LowStockThresholdLiters).FirstOrDefaultAsync(ct);

        // Активная химия с суммарным остатком по всем складам и признаком наличия строки баланса.
        var chemicals = await _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active)
            .Select(i => new
            {
                i.Id,
                i.Name,
                Total = _db.ChemicalStockBalances
                    .Where(b => b.ChemicalId == i.Id)
                    .Sum(b => (decimal?)b.TotalLiters) ?? 0m,
                HasBalance = _db.ChemicalStockBalances.Any(b => b.ChemicalId == i.Id),
            })
            .ToListAsync(ct);

        // «Закончилась» — химия, у которой был остаток на складе, а теперь ноль (ТЗ §22).
        var empty = chemicals
            .Where(c => c.HasBalance && c.Total <= 0)
            .OrderBy(c => c.Name)
            .Select(c => new DashboardStockDto(c.Id, c.Name, c.Total, StockStatus.Empty))
            .ToList();

        var low = chemicals
            .Where(c => c.Total > 0 && c.Total < threshold)
            .OrderBy(c => c.Total)
            .ThenBy(c => c.Name)
            .Select(c => new DashboardStockDto(c.Id, c.Name, c.Total, StockStatus.Low))
            .ToList();

        // Полный список активной химии для дашборда (по имени), со статусом остатка.
        var allChemicals = chemicals
            .OrderBy(c => c.Name)
            .Select(c => new DashboardStockDto(
                c.Id,
                c.Name,
                c.Total,
                c.HasBalance && c.Total <= 0 ? StockStatus.Empty
                    : c.Total > 0 && c.Total < threshold ? StockStatus.Low
                    : StockStatus.InStock))
            .ToList();

        var warehouses = await _db.Warehouses.CountAsync(ct);

        var recent = await _db.InventoryMovements.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.OccurredAt)
            .Take(RecentOperationsLimit)
            .Select(m => new HistoryItemDto(
                m.Id,
                m.OccurredAt,
                m.MovementType,
                m.ChemicalId,
                m.Chemical.Name,
                m.QuantityLiters,
                m.WarehouseId,
                m.Warehouse.Number,
                m.TargetWarehouseId,
                m.TargetWarehouse != null ? m.TargetWarehouse.Number : null,
                m.CropId,
                m.Crop != null ? m.Crop.Name : null,
                m.FieldId,
                m.Field != null ? m.Field.Number : null,
                _db.FieldTreatments
                    .Where(t => t.MovementId == m.Id)
                    .Select(t => (Guid?)t.Id)
                    .FirstOrDefault(),
                m.Comment))
            .ToListAsync(ct);

        return new DashboardDto(
            ActiveChemicals: chemicals.Count,
            Warehouses: warehouses,
            EmptyCount: empty.Count,
            LowCount: low.Count,
            TotalLiters: chemicals.Sum(c => c.Total),
            LowStockThresholdLiters: threshold,
            Empty: empty,
            Low: low,
            Chemicals: allChemicals,
            RecentOperations: recent);
    }

    public async Task<AllCompaniesDashboardDto> GetAllAsync(
        AllCompaniesDashboardQuery query, CancellationToken ct = default)
    {
        var threshold = await _db.AppSettings.Select(s => s.LowStockThresholdLiters).FirstOrDefaultAsync(ct);
        var accessible = await _companyContext.GetAccessibleCompaniesAsync(ct);
        if (accessible.Count == 0)
            return EmptyAll(threshold);

        var readable = new List<AccessibleCompany>();
        var accessByCompany = new Dictionary<Guid, CompanyAccess>();
        foreach (var company in accessible)
        {
            var access = await _companyContext.RequireForCompanyAsync(company.CompanyId, ct);
            if (!access.Has(Permissions.ReportsView)) continue;
            readable.Add(company);
            accessByCompany[company.CompanyId] = access;
        }
        if (readable.Count == 0)
            return EmptyAll(threshold);

        var companyIds = readable.Select(c => c.CompanyId).ToList();
        var companyNames = readable.ToDictionary(c => c.CompanyId, c => c.Name);

        var stockRows = await _db.ChemicalStockBalances.IgnoreQueryFilters()
            .Where(b => companyIds.Contains(b.CompanyId)
                        && b.Chemical.ItemType == ItemType.Chemical
                        && b.Chemical.Status == ItemStatus.Active)
            .Select(b => new StockRow(
                b.CompanyId,
                b.ChemicalId,
                b.Chemical.Name,
                b.WarehouseId,
                b.TotalLiters))
            .ToListAsync(ct);
        stockRows = stockRows
            .Where(r => accessByCompany[r.CompanyId].CanAccessWarehouse(r.WarehouseId))
            .ToList();

        var activeChemicals = await _db.InventoryItems.IgnoreQueryFilters()
            .Where(i => companyIds.Contains(i.CompanyId)
                        && i.ItemType == ItemType.Chemical
                        && i.Status == ItemStatus.Active)
            .Select(i => new { i.CompanyId, i.Id })
            .ToListAsync(ct);

        var warehouses = await _db.Warehouses.IgnoreQueryFilters()
            .Where(w => companyIds.Contains(w.CompanyId))
            .Select(w => new { w.CompanyId, w.Id })
            .ToListAsync(ct);
        warehouses = warehouses
            .Where(w => accessByCompany[w.CompanyId].CanAccessWarehouse(w.Id))
            .ToList();

        var movementsQuery = _db.InventoryMovements.IgnoreQueryFilters().AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId) && !m.IsDeleted);
        if (query.DateFrom is { } from) movementsQuery = movementsQuery.Where(m => m.OccurredAt >= from);
        if (query.DateTo is { } to) movementsQuery = movementsQuery.Where(m => m.OccurredAt <= to);

        var movements = await movementsQuery
            .Select(m => new MovementRow(
                m.Id,
                m.CompanyId,
                m.OccurredAt,
                m.MovementType,
                m.ChemicalId,
                m.Chemical.Name,
                m.QuantityLiters,
                m.WarehouseId,
                m.Warehouse.Number,
                m.TargetWarehouseId,
                m.TargetWarehouse != null ? m.TargetWarehouse.Number : null,
                m.CropId,
                m.Crop != null ? m.Crop.Name : null,
                m.FieldId,
                m.Field != null ? m.Field.Number : null,
                _db.FieldTreatments
                    .Where(t => t.MovementId == m.Id)
                    .Select(t => (Guid?)t.Id)
                    .FirstOrDefault(),
                m.Comment))
            .ToListAsync(ct);
        movements = movements
            .Where(m => accessByCompany[m.CompanyId].CanAccessWarehouse(m.WarehouseId)
                        && (m.TargetWarehouseId is null || accessByCompany[m.CompanyId].CanAccessWarehouse(m.TargetWarehouseId.Value)))
            .ToList();

        var stockByChemical = stockRows
            .GroupBy(r => new { r.CompanyId, r.ChemicalId, r.ChemicalName })
            .Select(g => new CompanyChemicalStock(
                g.Key.CompanyId, g.Key.ChemicalId, g.Key.ChemicalName, g.Sum(x => x.TotalLiters)))
            .ToList();

        var low = stockByChemical
            .Where(c => c.TotalLiters > 0 && c.TotalLiters < threshold)
            .OrderBy(c => c.TotalLiters)
            .ThenBy(c => c.ChemicalName)
            .Select(c => new AllCompaniesDashboardAlertDto(
                c.CompanyId, companyNames[c.CompanyId], c.ChemicalId, c.ChemicalName, c.TotalLiters, StockStatus.Low))
            .ToList();
        var empty = stockByChemical
            .Where(c => c.TotalLiters <= 0)
            .OrderBy(c => companyNames[c.CompanyId])
            .ThenBy(c => c.ChemicalName)
            .Select(c => new AllCompaniesDashboardAlertDto(
                c.CompanyId, companyNames[c.CompanyId], c.ChemicalId, c.ChemicalName, c.TotalLiters, StockStatus.Empty))
            .ToList();

        var companyRows = readable
            .OrderBy(c => c.Name)
            .Select(company =>
            {
                var cid = company.CompanyId;
                var companyMovements = movements.Where(m => m.CompanyId == cid).ToList();
                var companyStock = stockByChemical.Where(s => s.CompanyId == cid).ToList();
                return new AllCompaniesDashboardCompanyDto(
                    cid,
                    company.Name,
                    activeChemicals.Where(i => i.CompanyId == cid).Select(i => i.Id).Distinct().Count(),
                    warehouses.Where(w => w.CompanyId == cid).Select(w => w.Id).Distinct().Count(),
                    companyStock.Sum(s => s.TotalLiters),
                    companyMovements.Where(m => m.MovementType == MovementType.Income).Sum(m => m.QuantityLiters),
                    companyMovements.Where(m => m.MovementType == MovementType.Outcome).Sum(m => m.QuantityLiters),
                    companyStock.Count(s => s.TotalLiters > 0 && s.TotalLiters < threshold),
                    companyStock.Count(s => s.TotalLiters <= 0));
            })
            .ToList();

        var recent = movements
            .OrderByDescending(m => m.OccurredAt)
            .Take(RecentOperationsLimit)
            .Select(m => new HistoryItemDto(
                m.Id,
                m.OccurredAt,
                m.MovementType,
                m.ChemicalId,
                m.ChemicalName,
                m.QuantityLiters,
                m.WarehouseId,
                m.WarehouseNumber,
                m.TargetWarehouseId,
                m.TargetWarehouseNumber,
                m.CropId,
                m.CropName,
                m.FieldId,
                m.FieldNumber,
                m.FieldTreatmentId,
                m.Comment))
            .ToList();

        return new AllCompaniesDashboardDto(
            CompaniesCount: companyRows.Count,
            ActiveChemicals: companyRows.Sum(c => c.ActiveChemicals),
            Warehouses: companyRows.Sum(c => c.Warehouses),
            TotalLiters: companyRows.Sum(c => c.TotalLiters),
            IncomeLiters: companyRows.Sum(c => c.IncomeLiters),
            OutcomeLiters: companyRows.Sum(c => c.OutcomeLiters),
            LowCount: low.Count,
            EmptyCount: empty.Count,
            LowStockThresholdLiters: threshold,
            Companies: companyRows,
            Low: low,
            Empty: empty,
            RecentOperations: recent);
    }

    private static AllCompaniesDashboardDto EmptyAll(decimal threshold) => new(
        0, 0, 0, 0, 0, 0, 0, 0, threshold,
        Array.Empty<AllCompaniesDashboardCompanyDto>(),
        Array.Empty<AllCompaniesDashboardAlertDto>(),
        Array.Empty<AllCompaniesDashboardAlertDto>(),
        Array.Empty<HistoryItemDto>());

    private sealed record StockRow(
        Guid CompanyId, Guid ChemicalId, string ChemicalName, Guid WarehouseId, decimal TotalLiters);

    private sealed record CompanyChemicalStock(
        Guid CompanyId, Guid ChemicalId, string ChemicalName, decimal TotalLiters);

    private sealed record MovementRow(
        Guid Id,
        Guid CompanyId,
        DateTimeOffset OccurredAt,
        MovementType MovementType,
        Guid ChemicalId,
        string ChemicalName,
        decimal QuantityLiters,
        Guid WarehouseId,
        string WarehouseNumber,
        Guid? TargetWarehouseId,
        string? TargetWarehouseNumber,
        Guid? CropId,
        string? CropName,
        Guid? FieldId,
        string? FieldNumber,
        Guid? FieldTreatmentId,
        string? Comment);
}
