using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

/// <summary>
/// Раздел «Приходы» по доступным хозяйствам. Читает несколько хозяйств осознанно:
/// доступ ограничивается членствами, правом receipts.view и областью складов.
/// </summary>
public sealed class ReceiptQueryService
{
    private readonly IApplicationDbContext _db;
    private readonly CompanyContextService _companyContext;

    public ReceiptQueryService(IApplicationDbContext db, CompanyContextService companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    public async Task<IReadOnlyList<ReceiptItemDto>> GetAsync(ReceiptQuery query, CancellationToken ct = default)
    {
        var accessible = await _companyContext.GetAccessibleCompaniesAsync(ct);
        if (accessible.Count == 0) return Array.Empty<ReceiptItemDto>();

        var readable = new List<AccessibleCompany>();
        var accessByCompany = new Dictionary<Guid, CompanyAccess>();
        foreach (var company in accessible)
        {
            var access = await _companyContext.RequireForCompanyAsync(company.CompanyId, ct);
            if (!access.Has(Permissions.ReceiptsView)) continue;
            readable.Add(company);
            accessByCompany[company.CompanyId] = access;
        }

        if (query.CompanyId is { } requestedCompany)
            readable = readable.Where(c => c.CompanyId == requestedCompany).ToList();
        if (readable.Count == 0) return Array.Empty<ReceiptItemDto>();

        var companyNames = readable.ToDictionary(c => c.CompanyId, c => c.Name);
        var companyIds = readable.Select(c => c.CompanyId).ToList();

        var q = _db.InventoryMovements.IgnoreQueryFilters().AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId)
                        && !m.IsDeleted
                        && m.MovementType == MovementType.Income);
        if (query.DateFrom is { } from) q = q.Where(m => m.OccurredAt >= from);
        if (query.DateTo is { } to) q = q.Where(m => m.OccurredAt <= to);
        if (query.CanonicalChemicalId is { } canonical)
            q = q.Where(m => m.Chemical.CanonicalChemicalId == canonical);

        var rows = await q
            .OrderByDescending(m => m.OccurredAt)
            .Select(m => new
            {
                m.Id,
                m.OccurredAt,
                m.CompanyId,
                m.ChemicalId,
                ChemicalName = m.Chemical.Name,
                m.Chemical.CanonicalChemicalId,
                CanonicalChemicalName = m.Chemical.CanonicalChemical != null
                    ? m.Chemical.CanonicalChemical.CanonicalName
                    : null,
                m.QuantityLiters,
                m.UnitType,
                m.PackageVolumeLiters,
                m.PackagesQuantity,
                m.WarehouseId,
                WarehouseNumber = m.Warehouse.Number,
                m.Comment,
            })
            .ToListAsync(ct);

        return rows
            .Where(r => accessByCompany[r.CompanyId].CanAccessWarehouse(r.WarehouseId))
            .Select(r => new ReceiptItemDto(
                r.Id,
                r.OccurredAt,
                r.CompanyId,
                companyNames[r.CompanyId],
                r.ChemicalId,
                r.ChemicalName,
                r.CanonicalChemicalId,
                r.CanonicalChemicalName,
                r.QuantityLiters,
                r.UnitType,
                r.PackageVolumeLiters,
                r.PackagesQuantity,
                r.WarehouseId,
                r.WarehouseNumber,
                r.Comment))
            .ToList();
    }
}
