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
        var thresholds = await LoadThresholdsAsync(ct);

        // Активная химия с суммарным остатком по всем складам и признаком наличия строки баланса.
        var chemicals = await _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.MeasureUnit,
                Total = _db.ChemicalStockBalances
                    .Where(b => b.ChemicalId == i.Id)
                    .Sum(b => (decimal?)b.TotalQuantity) ?? 0m,
                HasBalance = _db.ChemicalStockBalances.Any(b => b.ChemicalId == i.Id),
            })
            .ToListAsync(ct);

        // «Закончилась» — химия, у которой был остаток на складе, а теперь ноль (ТЗ §22).
        var empty = chemicals
            .Where(c => c.HasBalance && c.Total <= 0)
            .OrderBy(c => c.Name)
            .Select(c => new DashboardStockDto(c.Id, c.Name, c.MeasureUnit, c.Total, StockStatus.Empty))
            .ToList();

        var low = chemicals
            .Where(c => c.Total > 0 && c.Total < thresholds.For(c.MeasureUnit))
            .OrderBy(c => c.Total)
            .ThenBy(c => c.Name)
            .Select(c => new DashboardStockDto(c.Id, c.Name, c.MeasureUnit, c.Total, StockStatus.Low))
            .ToList();

        // Полный список активной химии для дашборда (по имени), со статусом остатка.
        var allChemicals = chemicals
            .OrderBy(c => c.Name)
            .Select(c => new DashboardStockDto(
                c.Id,
                c.Name,
                c.MeasureUnit,
                c.Total,
                c.HasBalance && c.Total <= 0 ? StockStatus.Empty
                    : c.Total > 0 && c.Total < thresholds.For(c.MeasureUnit) ? StockStatus.Low
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
                m.Quantity,
                m.Chemical.MeasureUnit,
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
            LowStockThresholdLiters: thresholds.Liters,
            LowStockThresholdKg: thresholds.Kg,
            Empty: empty,
            Low: low,
            Chemicals: allChemicals,
            RecentOperations: recent);
    }

    public async Task<AllCompaniesDashboardDto> GetAllAsync(
        AllCompaniesDashboardQuery query, CancellationToken ct = default)
    {
        var thresholds = await LoadThresholdsAsync(ct);
        var accessible = await _companyContext.GetAccessibleCompaniesAsync(ct);
        if (accessible.Count == 0)
            return EmptyAll(thresholds);

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
            return EmptyAll(thresholds);

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
                b.Chemical.MeasureUnit,
                b.WarehouseId,
                b.TotalQuantity))
            .ToListAsync(ct);
        stockRows = stockRows
            .Where(r => accessByCompany[r.CompanyId].CanAccessWarehouse(r.WarehouseId))
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
                m.Chemical.MeasureUnit,
                m.Quantity,
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
            .GroupBy(r => new { r.CompanyId, r.ChemicalId, r.ChemicalName, r.MeasureUnit })
            .Select(g => new CompanyChemicalStock(
                g.Key.CompanyId, g.Key.ChemicalId, g.Key.ChemicalName, g.Key.MeasureUnit, g.Sum(x => x.TotalQuantity)))
            .ToList();

        var low = stockByChemical
            .Where(c => c.TotalQuantity > 0 && c.TotalQuantity < thresholds.For(c.MeasureUnit))
            .OrderBy(c => c.TotalQuantity)
            .ThenBy(c => c.ChemicalName)
            .Select(c => new AllCompaniesDashboardAlertDto(
                c.CompanyId, companyNames[c.CompanyId], c.ChemicalId, c.ChemicalName, c.MeasureUnit, c.TotalQuantity, StockStatus.Low))
            .ToList();
        var empty = stockByChemical
            .Where(c => c.TotalQuantity <= 0)
            .OrderBy(c => companyNames[c.CompanyId])
            .ThenBy(c => c.ChemicalName)
            .Select(c => new AllCompaniesDashboardAlertDto(
                c.CompanyId, companyNames[c.CompanyId], c.ChemicalId, c.ChemicalName, c.MeasureUnit, c.TotalQuantity, StockStatus.Empty))
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
                m.Quantity,
                m.MeasureUnit,
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
            LowStockThresholdLiters: thresholds.Liters,
            LowStockThresholdKg: thresholds.Kg,
            Low: low,
            Empty: empty,
            RecentOperations: recent);
    }

    private async Task<Thresholds> LoadThresholdsAsync(CancellationToken ct)
    {
        var s = await _db.AppSettings
            .Select(s => new { s.LowStockThresholdLiters, s.LowStockThresholdKg })
            .FirstOrDefaultAsync(ct);
        return new Thresholds(s?.LowStockThresholdLiters ?? 0m, s?.LowStockThresholdKg ?? 0m);
    }

    private static AllCompaniesDashboardDto EmptyAll(Thresholds thresholds) => new(
        thresholds.Liters,
        thresholds.Kg,
        Array.Empty<AllCompaniesDashboardAlertDto>(),
        Array.Empty<AllCompaniesDashboardAlertDto>(),
        Array.Empty<HistoryItemDto>());

    private readonly record struct Thresholds(decimal Liters, decimal Kg)
    {
        public decimal For(MeasureUnit unit) => unit == MeasureUnit.Kilogram ? Kg : Liters;
    }

    private sealed record StockRow(
        Guid CompanyId, Guid ChemicalId, string ChemicalName, MeasureUnit MeasureUnit, Guid WarehouseId, decimal TotalQuantity);

    private sealed record CompanyChemicalStock(
        Guid CompanyId, Guid ChemicalId, string ChemicalName, MeasureUnit MeasureUnit, decimal TotalQuantity);

    private sealed record MovementRow(
        Guid Id,
        Guid CompanyId,
        DateTimeOffset OccurredAt,
        MovementType MovementType,
        Guid ChemicalId,
        string ChemicalName,
        MeasureUnit MeasureUnit,
        decimal Quantity,
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
