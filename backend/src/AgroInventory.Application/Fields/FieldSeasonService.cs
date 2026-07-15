using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Fields;

public sealed class FieldSeasonService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public FieldSeasonService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        CompanyContextService companyContext,
        IAuditLogger audit,
        TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<FieldSeasonDto>> GetAllAsync(Guid? fieldId = null, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireAsync(ct);
        var query = _db.FieldSeasons.AsNoTracking().AsQueryable();
        if (!access.HasFullScope)
            query = query.Where(s => access.FieldIds.Contains(s.FieldId));
        if (fieldId is { } id)
        {
            access.RequireField(id);
            query = query.Where(s => s.FieldId == id);
        }

        return await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.StartedAt)
            .Select(s => ToDto(s))
            .ToListAsync(ct);
    }

    public async Task<FieldSeasonDto> CreateAsync(CreateFieldSeasonRequest request, CancellationToken ct = default)
    {
        await ValidateAsync(request.FieldId, request.CropId, request.Year, request.StartedAt, request.FinishedAt, ct);
        var now = _clock.GetUtcNow();
        var season = new FieldSeason
        {
            Id = Guid.NewGuid(),
            CompanyId = _currentUser.CompanyId,
            FieldId = request.FieldId,
            CropId = request.CropId,
            Year = request.Year,
            Name = Trim(request.Name),
            StartedAt = request.StartedAt,
            FinishedAt = request.FinishedAt,
            Comment = Trim(request.Comment),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.FieldSeasons.Add(season);
        _audit.Log(AuditAction.Create, "FieldSeason", season.Id, null,
            new { season.FieldId, season.CropId, season.Year, season.Name, season.StartedAt, season.FinishedAt });
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(season.Id, ct);
    }

    public async Task<FieldSeasonDto> UpdateAsync(Guid id, UpdateFieldSeasonRequest request, CancellationToken ct = default)
    {
        var season = await _db.FieldSeasons.FirstOrDefaultAsync(s => s.Id == id, ct)
                     ?? throw NotFoundException.For("Сезон поля", id);
        await ValidateAsync(season.FieldId, request.CropId, request.Year, request.StartedAt, request.FinishedAt, ct);

        season.CropId = request.CropId;
        season.Year = request.Year;
        season.Name = Trim(request.Name);
        season.StartedAt = request.StartedAt;
        season.FinishedAt = request.FinishedAt;
        season.Comment = Trim(request.Comment);
        season.UpdatedAt = _clock.GetUtcNow();
        _audit.Log(AuditAction.Update, "FieldSeason", season.Id, null,
            new { season.FieldId, season.CropId, season.Year, season.Name, season.StartedAt, season.FinishedAt });
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task ValidateAsync(
        Guid fieldId, Guid cropId, int year, DateTimeOffset? startedAt, DateTimeOffset? finishedAt, CancellationToken ct)
    {
        if (year < 2000 || year > 2100)
            throw new ValidationException("year", "Укажите корректный год сезона.");
        if (startedAt is not null && finishedAt is not null && finishedAt < startedAt)
            throw new ValidationException("finishedAt", "Дата окончания не может быть раньше начала.");

        var access = await _companyContext.RequireAsync(ct);
        access.RequireField(fieldId);
        if (!await _db.Fields.AnyAsync(f => f.Id == fieldId, ct))
            throw NotFoundException.For("Поле", fieldId);
        if (!await _db.Crops.AnyAsync(c => c.Id == cropId, ct))
            throw NotFoundException.For("Культура", cropId);
    }

    private async Task<FieldSeasonDto> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.FieldSeasons
            .Where(s => s.Id == id)
            .Select(s => ToDto(s))
            .FirstAsync(ct);

    private static FieldSeasonDto ToDto(FieldSeason s) => new(
        s.Id,
        s.FieldId,
        s.Field.Number,
        s.CropId,
        s.Crop.Name,
        s.Year,
        s.Name,
        s.StartedAt,
        s.FinishedAt,
        s.Comment);

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
