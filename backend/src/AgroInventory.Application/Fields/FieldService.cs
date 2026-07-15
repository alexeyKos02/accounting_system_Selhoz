using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Fields;

/// <summary>Справочник полей/участков: список, быстрое добавление, редактирование.</summary>
public sealed class FieldService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly TimeProvider _clock;

    public FieldService(
        IApplicationDbContext db, ICurrentUser currentUser,
        CompanyContextService companyContext, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
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
            .Select(f => new FieldDto(f.Id, f.Number))
            .ToListAsync(ct);
    }

    public async Task<FieldDto> CreateAsync(CreateFieldRequest request, CancellationToken ct = default)
    {
        var number = (request.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
            throw new ValidationException(nameof(request.Number), "Номер поля обязателен.");

        if (await _db.Fields.AnyAsync(f => f.Number.ToLower() == number.ToLower(), ct))
            throw new ConflictException($"Поле «{number}» уже существует.");

        var now = _clock.GetUtcNow();
        // company_id — выбранное хозяйство (валидировано CompanyContextService, ТЗ §7, §24).
        var field = new Field
        {
            Id = Guid.NewGuid(),
            CompanyId = _currentUser.CompanyId,
            Number = number,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Fields.Add(field);
        await _db.SaveChangesAsync(ct);
        return new FieldDto(field.Id, field.Number);
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

        field.Number = number;
        field.UpdatedAt = _clock.GetUtcNow();
        await _db.SaveChangesAsync(ct);
        return new FieldDto(field.Id, field.Number);
    }
}
