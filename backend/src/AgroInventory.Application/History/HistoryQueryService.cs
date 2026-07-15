using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.History;

/// <summary>Чтение истории складских операций (ТЗ §19). Удалённые (is_deleted) не показываются.</summary>
public sealed class HistoryQueryService
{
    private readonly IApplicationDbContext _db;

    public HistoryQueryService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<HistoryItemDto>> GetAsync(HistoryQuery query, CancellationToken ct = default)
    {
        var q = Filter(_db.InventoryMovements.AsNoTracking(), query);

        return await q
            .OrderByDescending(m => m.OccurredAt)
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
    }

    public async Task<HistoryDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _db.InventoryMovements.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new
            {
                x.Id, x.OccurredAt, x.MovementType, x.ChemicalId, ChemicalName = x.Chemical.Name,
                x.QuantityLiters, x.UnitType, x.PackageVolumeLiters, x.PackagesQuantity,
                x.WarehouseId, WarehouseNumber = x.Warehouse.Number,
                x.TargetWarehouseId, TargetWarehouseNumber = x.TargetWarehouse != null ? x.TargetWarehouse.Number : null,
                x.CropId, CropName = x.Crop != null ? x.Crop.Name : null,
                x.FieldId, FieldNumber = x.Field != null ? x.Field.Number : null,
                FieldTreatmentId = _db.FieldTreatments
                    .Where(t => t.MovementId == x.Id)
                    .Select(t => (Guid?)t.Id)
                    .FirstOrDefault(),
                x.Comment,
                Sources = x.Details.Select(d => new HistoryDetailSourceDto(
                    d.SourceType, d.UnitType, d.PackageVolumeLiters, d.QuantityLiters, d.PackagesQuantity)).ToList(),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.For("Операция", id);

        return new HistoryDetailDto(
            m.Id, m.OccurredAt, m.MovementType, m.ChemicalId, m.ChemicalName, m.QuantityLiters,
            m.UnitType, m.PackageVolumeLiters, m.PackagesQuantity, m.WarehouseId, m.WarehouseNumber,
            m.TargetWarehouseId, m.TargetWarehouseNumber, m.CropId, m.CropName, m.FieldId, m.FieldNumber,
            m.FieldTreatmentId, m.Comment, m.Sources);
    }

    private static IQueryable<InventoryMovement> Filter(IQueryable<InventoryMovement> q, HistoryQuery f)
    {
        q = q.Where(m => !m.IsDeleted);
        if (f.DateFrom is { } from) q = q.Where(m => m.OccurredAt >= from);
        if (f.DateTo is { } to) q = q.Where(m => m.OccurredAt <= to);
        if (f.ChemicalId is { } chem) q = q.Where(m => m.ChemicalId == chem);
        if (f.MovementType is { } type) q = q.Where(m => m.MovementType == type);
        if (f.WarehouseId is { } wh) q = q.Where(m => m.WarehouseId == wh);
        if (f.CropId is { } crop) q = q.Where(m => m.CropId == crop);
        if (f.FieldId is { } field) q = q.Where(m => m.FieldId == field);
        return q;
    }
}
