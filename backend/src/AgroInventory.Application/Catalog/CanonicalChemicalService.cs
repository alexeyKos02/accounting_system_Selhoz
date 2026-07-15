using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Catalog;

/// <summary>
/// Общий канонический справочник препаратов (ТЗ §12). Глобальный (без хозяйства). Чтение доступно
/// любому авторизованному (для привязки карточек и общего режима §17); ведёт справочник только
/// SystemAdmin — проверка политики в контроллере.
/// </summary>
public sealed class CanonicalChemicalService
{
    private const string EntityType = "CanonicalChemical";

    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public CanonicalChemicalService(IApplicationDbContext db, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    /// <summary>Список/поиск канонических препаратов (для выпадающего списка привязки).</summary>
    public async Task<IReadOnlyList<CanonicalChemicalDto>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.CanonicalChemicals.AsQueryable();
        var term = (search ?? string.Empty).Trim().ToLower();
        if (term.Length > 0)
            query = query.Where(c => c.CanonicalName.ToLower().Contains(term));

        return await query
            .OrderBy(c => c.CanonicalName)
            .Select(c => new CanonicalChemicalDto(
                c.Id, c.CanonicalName, c.Manufacturer, c.ActiveIngredient, c.Concentration, c.Formulation, c.RegistrationNumber))
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<CanonicalChemicalDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.CanonicalChemicals.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw NotFoundException.For("Канонический препарат", id);
        return ToDto(c);
    }

    public async Task<CanonicalChemicalDto> CreateAsync(CreateCanonicalChemicalRequest request, CancellationToken ct = default)
    {
        var name = (request.CanonicalName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ValidationException(nameof(request.CanonicalName), "Название препарата обязательно.");

        var now = _clock.GetUtcNow();
        var item = new CanonicalChemical
        {
            Id = Guid.NewGuid(),
            CanonicalName = name,
            Manufacturer = Trim(request.Manufacturer),
            ActiveIngredient = Trim(request.ActiveIngredient),
            Concentration = Trim(request.Concentration),
            Formulation = Trim(request.Formulation),
            RegistrationNumber = Trim(request.RegistrationNumber),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.CanonicalChemicals.Add(item);

        _audit.Log(AuditAction.Create, EntityType, item.Id, null, new { item.CanonicalName, item.Manufacturer });
        await _db.SaveChangesAsync(ct);

        return ToDto(item);
    }

    public async Task<CanonicalChemicalDto> UpdateAsync(Guid id, UpdateCanonicalChemicalRequest request, CancellationToken ct = default)
    {
        var item = await _db.CanonicalChemicals.FirstOrDefaultAsync(x => x.Id == id, ct)
                   ?? throw NotFoundException.For("Канонический препарат", id);

        var name = (request.CanonicalName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ValidationException(nameof(request.CanonicalName), "Название препарата обязательно.");

        var old = new { item.CanonicalName, item.Manufacturer };

        item.CanonicalName = name;
        item.Manufacturer = Trim(request.Manufacturer);
        item.ActiveIngredient = Trim(request.ActiveIngredient);
        item.Concentration = Trim(request.Concentration);
        item.Formulation = Trim(request.Formulation);
        item.RegistrationNumber = Trim(request.RegistrationNumber);
        item.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Update, EntityType, item.Id, old, new { item.CanonicalName, item.Manufacturer });
        await _db.SaveChangesAsync(ct);

        return ToDto(item);
    }

    private static CanonicalChemicalDto ToDto(CanonicalChemical c) => new(
        c.Id, c.CanonicalName, c.Manufacturer, c.ActiveIngredient, c.Concentration, c.Formulation, c.RegistrationNumber);

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
