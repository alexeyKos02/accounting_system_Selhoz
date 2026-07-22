using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Chemicals;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Catalog;

/// <summary>
/// Общий канонический справочник препаратов (ТЗ §12). Глобальный (без хозяйства). Чтение доступно
/// любому авторизованному (для привязки карточек и общего режима §17); ведёт справочник только
/// SystemAdmin — проверка политики в контроллере. Набор полей повторяет карточку химии.
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
                c.Id, c.CanonicalName, c.Type, c.MeasureUnit, c.Manufacturer, c.Comment,
                c.CanonicalChemicalCrops.Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name)).ToList()))
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<CanonicalChemicalDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.CanonicalChemicals
                    .Include(x => x.CanonicalChemicalCrops).ThenInclude(cc => cc.Crop)
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw NotFoundException.For("Канонический препарат", id);
        return ToDto(c);
    }

    public async Task<CanonicalChemicalDto> CreateAsync(CreateCanonicalChemicalRequest request, CancellationToken ct = default)
    {
        var name = (request.CanonicalName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ValidationException(nameof(request.CanonicalName), "Название препарата обязательно.");

        // Защита от дублей по названию (§12). Сравнение в памяти через OrdinalIgnoreCase —
        // корректно сворачивает регистр кириллицы независимо от коллации БД (в отличие от SQL lower()).
        var existingNames = await _db.CanonicalChemicals.Select(c => c.CanonicalName).ToListAsync(ct);
        if (existingNames.Any(n => string.Equals(n.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Препарат «{name}» уже есть в общем каталоге.");

        var cropIds = await ValidateCropsAsync(request.CropIds, ct);

        var now = _clock.GetUtcNow();
        var item = new CanonicalChemical
        {
            Id = Guid.NewGuid(),
            CanonicalName = name,
            Type = request.Type,
            MeasureUnit = request.MeasureUnit,
            Manufacturer = Trim(request.Manufacturer),
            Comment = Trim(request.Comment),
            CanonicalChemicalCrops = cropIds.Select(cid => new CanonicalChemicalCrop { CropId = cid }).ToList(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.CanonicalChemicals.Add(item);

        _audit.Log(AuditAction.Create, EntityType, item.Id, null, new { item.CanonicalName, item.Manufacturer });
        await _db.SaveChangesAsync(ct);

        return await GetAsync(item.Id, ct);
    }

    public async Task<CanonicalChemicalDto> UpdateAsync(Guid id, UpdateCanonicalChemicalRequest request, CancellationToken ct = default)
    {
        var item = await _db.CanonicalChemicals
                       .Include(x => x.CanonicalChemicalCrops)
                       .FirstOrDefaultAsync(x => x.Id == id, ct)
                   ?? throw NotFoundException.For("Канонический препарат", id);

        var name = (request.CanonicalName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ValidationException(nameof(request.CanonicalName), "Название препарата обязательно.");

        var cropIds = await ValidateCropsAsync(request.CropIds, ct);

        var old = new { item.CanonicalName, item.Manufacturer };

        item.CanonicalName = name;
        item.Type = request.Type;
        item.MeasureUnit = request.MeasureUnit;
        item.Manufacturer = Trim(request.Manufacturer);
        item.Comment = Trim(request.Comment);
        item.UpdatedAt = _clock.GetUtcNow();
        ReconcileCrops(item, cropIds);

        _audit.Log(AuditAction.Update, EntityType, item.Id, old, new { item.CanonicalName, item.Manufacturer });
        await _db.SaveChangesAsync(ct);

        return await GetAsync(item.Id, ct);
    }

    private static CanonicalChemicalDto ToDto(CanonicalChemical c) => new(
        c.Id, c.CanonicalName, c.Type, c.MeasureUnit, c.Manufacturer, c.Comment,
        c.CanonicalChemicalCrops.Select(cc => new CropRefDto(cc.CropId, cc.Crop.Name)).ToList());

    /// <summary>Проверяет существование культур. Для глобального каталога культуры необязательны.</summary>
    private async Task<List<Guid>> ValidateCropsAsync(IReadOnlyList<Guid>? cropIds, CancellationToken ct)
    {
        var ids = (cropIds ?? Array.Empty<Guid>()).Distinct().ToList();
        if (ids.Count == 0)
            return ids;

        var existing = await _db.Crops.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        if (existing.Count != ids.Count)
            throw new ValidationException(nameof(CreateCanonicalChemicalRequest.CropIds), "Некоторые культуры не найдены.");
        return ids;
    }

    private static void ReconcileCrops(CanonicalChemical item, List<Guid> cropIds)
    {
        foreach (var link in item.CanonicalChemicalCrops.Where(cc => !cropIds.Contains(cc.CropId)).ToList())
            item.CanonicalChemicalCrops.Remove(link);

        var existingIds = item.CanonicalChemicalCrops.Select(cc => cc.CropId).ToHashSet();
        foreach (var cid in cropIds.Where(cid => !existingIds.Contains(cid)))
            item.CanonicalChemicalCrops.Add(new CanonicalChemicalCrop { CanonicalChemicalId = item.Id, CropId = cid });
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
