using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Inventory;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Fields;

/// <summary>Обработки полей: фиксирует работу на поле и создаёт складское списание химии.</summary>
public sealed class FieldTreatmentService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly InventoryService _inventory;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public FieldTreatmentService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        CompanyContextService companyContext,
        InventoryService inventory,
        IAuditLogger audit,
        TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
        _inventory = inventory;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<FieldTreatmentDto>> GetAllAsync(Guid? fieldId = null, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireAsync(ct);
        var query = _db.FieldTreatments.AsNoTracking().AsQueryable();
        if (!access.HasFullScope)
            query = query.Where(t => access.FieldIds.Contains(t.FieldId));
        if (fieldId is { } id)
        {
            access.RequireField(id);
            query = query.Where(t => t.FieldId == id);
        }

        return await query
            .OrderByDescending(t => t.TreatedAt)
            .Select(t => new FieldTreatmentDto(
                t.Id,
                t.TreatedAt,
                t.FieldId,
                t.Field.Number,
                t.ChemicalId,
                t.Chemical.Name,
                t.Chemical.MeasureUnit,
                t.WarehouseId,
                t.Warehouse.Number,
                t.CropId,
                t.Crop.Name,
                t.Quantity,
                t.RatePerHectare,
                t.MovementId,
                t.Comment))
            .ToListAsync(ct);
    }

    public async Task<FieldTreatmentDto> CreateAsync(CreateFieldTreatmentRequest request, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireAsync(ct);
        access.RequireField(request.FieldId);
        access.RequireWarehouse(request.WarehouseId);

        var field = await _db.Fields.FirstOrDefaultAsync(f => f.Id == request.FieldId, ct)
                    ?? throw NotFoundException.For("Поле", request.FieldId);

        var quantity = ResolveQuantity(field, request);
        var treatedAt = request.TreatedAt ?? _clock.GetUtcNow();
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        var outcome = await _inventory.OutcomeAsync(new OutcomeRequest(
            request.ChemicalId,
            request.WarehouseId,
            request.CropId,
            quantity,
            treatedAt,
            comment is null ? "Обработка поля" : $"Обработка поля. {comment}",
            FieldId: request.FieldId), ct);

        var now = _clock.GetUtcNow();
        var treatment = new FieldTreatment
        {
            Id = Guid.NewGuid(),
            CompanyId = _currentUser.CompanyId,
            FieldId = request.FieldId,
            ChemicalId = request.ChemicalId,
            WarehouseId = request.WarehouseId,
            CropId = request.CropId,
            MovementId = outcome.MovementId,
            Quantity = quantity,
            RatePerHectare = request.RatePerHectare,
            TreatedAt = treatedAt,
            Comment = comment,
            CreatedByUserId = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.FieldTreatments.Add(treatment);
        _audit.Log(AuditAction.Create, "FieldTreatment", treatment.Id, null,
            new
            {
                treatment.FieldId,
                treatment.ChemicalId,
                treatment.WarehouseId,
                treatment.CropId,
                treatment.MovementId,
                treatment.Quantity,
                treatment.RatePerHectare,
                treatment.TreatedAt
            });
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(treatment.Id, ct);
    }

    private static decimal ResolveQuantity(Field field, CreateFieldTreatmentRequest request)
    {
        if (request.RatePerHectare is { } rate)
        {
            if (rate <= 0)
                throw new ValidationException(nameof(request.RatePerHectare), "Норма должна быть больше нуля.");
            if (field.AreaHectares is not { } area || area <= 0)
                throw new ValidationException(nameof(request.FieldId),
                    "Для расчёта по норме укажите площадь поля.");
            return rate * area;
        }

        if (request.Quantity is not { } quantity || quantity <= 0)
            throw new ValidationException(nameof(request.Quantity), "Укажите количество или норму на гектар.");
        return quantity;
    }

    private async Task<FieldTreatmentDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.FieldTreatments
            .Where(t => t.Id == id)
            .Select(t => new FieldTreatmentDto(
                t.Id,
                t.TreatedAt,
                t.FieldId,
                t.Field.Number,
                t.ChemicalId,
                t.Chemical.Name,
                t.Chemical.MeasureUnit,
                t.WarehouseId,
                t.Warehouse.Number,
                t.CropId,
                t.Crop.Name,
                t.Quantity,
                t.RatePerHectare,
                t.MovementId,
                t.Comment))
            .FirstAsync(ct);
    }
}
