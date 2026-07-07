using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Crops;

/// <summary>Справочник культур (ТЗ §7.3): список, быстрое добавление, редактирование.</summary>
public sealed class CropService
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public CropService(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CropDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Crops
            .OrderBy(c => c.Name)
            .Select(c => new CropDto(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<CropDto> CreateAsync(CreateCropRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationException(nameof(request.Name), "Название культуры обязательно.");

        if (await _db.Crops.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException($"Культура «{name}» уже существует.");

        var now = _clock.GetUtcNow();
        var crop = new Crop { Id = Guid.NewGuid(), Name = name, CreatedAt = now, UpdatedAt = now };
        _db.Crops.Add(crop);
        await _db.SaveChangesAsync(ct);
        return new CropDto(crop.Id, crop.Name);
    }

    public async Task<CropDto> UpdateAsync(Guid id, UpdateCropRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationException(nameof(request.Name), "Название культуры обязательно.");

        var crop = await _db.Crops.FirstOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw NotFoundException.For("Культура", id);

        if (await _db.Crops.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException($"Культура «{name}» уже существует.");

        crop.Name = name;
        crop.UpdatedAt = _clock.GetUtcNow();
        await _db.SaveChangesAsync(ct);
        return new CropDto(crop.Id, crop.Name);
    }
}
