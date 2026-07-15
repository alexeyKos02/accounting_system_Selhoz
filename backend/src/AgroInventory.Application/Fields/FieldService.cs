using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Fields;

/// <summary>Справочник полей/участков: список, быстрое добавление, редактирование.</summary>
public sealed class FieldService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public FieldService(
        IApplicationDbContext db, ICurrentUser currentUser,
        CompanyContextService companyContext, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<FieldDto>> GetAllAsync(CancellationToken ct = default)
    {
        // Ограничение по области доступа (ТЗ §6): при неполном scope — только разрешённые поля.
        var access = await _companyContext.RequireAsync(ct);
        var query = _db.Fields.AsQueryable();
        if (!access.HasFullScope)
            query = query.Where(f => access.FieldIds.Contains(f.Id));

        return await query
            .OrderBy(f => f.Number)
            .Select(f => new FieldDto(
                f.Id,
                f.Number,
                f.AreaHectares,
                f.CurrentCropId,
                f.CurrentCrop != null ? f.CurrentCrop.Name : null))
            .ToListAsync(ct);
    }

    public async Task<FieldDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireAsync(ct);
        if (!access.HasFullScope)
            access.RequireField(id);
        return await GetByIdAsync(id, ct);
    }

    public async Task<FieldDto> CreateAsync(CreateFieldRequest request, CancellationToken ct = default)
    {
        var number = (request.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
            throw new ValidationException(nameof(request.Number), "Номер поля обязателен.");

        if (await _db.Fields.AnyAsync(f => f.Number.ToLower() == number.ToLower(), ct))
            throw new ConflictException($"Поле «{number}» уже существует.");
        if (request.AreaHectares is < 0)
            throw new ValidationException(nameof(request.AreaHectares), "Площадь не может быть отрицательной.");
        if (request.CurrentCropId is { } cropId && !await _db.Crops.AnyAsync(c => c.Id == cropId, ct))
            throw NotFoundException.For("Культура", cropId);

        var now = _clock.GetUtcNow();
        // company_id — выбранное хозяйство (валидировано CompanyContextService, ТЗ §7, §24).
        var field = new Field
        {
            Id = Guid.NewGuid(),
            CompanyId = _currentUser.CompanyId,
            Number = number,
            AreaHectares = request.AreaHectares,
            CurrentCropId = request.CurrentCropId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Fields.Add(field);
        _audit.Log(AuditAction.Create, "Field", field.Id, null,
            new { field.Number, field.AreaHectares, field.CurrentCropId });
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(field.Id, ct);
    }

    public async Task<FieldDto> UpdateAsync(Guid id, UpdateFieldRequest request, CancellationToken ct = default)
    {
        var number = (request.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
            throw new ValidationException(nameof(request.Number), "Номер поля обязателен.");

        var field = await _db.Fields.FirstOrDefaultAsync(f => f.Id == id, ct)
                    ?? throw NotFoundException.For("Поле", id);

        if (await _db.Fields.AnyAsync(f => f.Id != id && f.Number.ToLower() == number.ToLower(), ct))
            throw new ConflictException($"Поле «{number}» уже существует.");
        if (request.AreaHectares is < 0)
            throw new ValidationException(nameof(request.AreaHectares), "Площадь не может быть отрицательной.");
        if (request.CurrentCropId is { } cropId && !await _db.Crops.AnyAsync(c => c.Id == cropId, ct))
            throw NotFoundException.For("Культура", cropId);

        var old = new { field.Number, field.AreaHectares, field.CurrentCropId };
        field.Number = number;
        field.AreaHectares = request.AreaHectares;
        field.CurrentCropId = request.CurrentCropId;
        field.UpdatedAt = _clock.GetUtcNow();
        _audit.Log(AuditAction.Update, "Field", field.Id, old,
            new { field.Number, field.AreaHectares, field.CurrentCropId });
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(field.Id, ct);
    }

    private async Task<FieldDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Fields
            .Where(f => f.Id == id)
            .Select(f => new FieldDto(
                f.Id,
                f.Number,
                f.AreaHectares,
                f.CurrentCropId,
                f.CurrentCrop != null ? f.CurrentCrop.Name : null))
            .FirstAsync(ct);
    }
}
