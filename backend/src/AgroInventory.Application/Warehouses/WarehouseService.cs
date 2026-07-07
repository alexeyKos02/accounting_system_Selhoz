using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Warehouses;

/// <summary>Справочник складов (ТЗ §7.4): список, быстрое добавление, редактирование.</summary>
public sealed class WarehouseService
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public WarehouseService(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Warehouses
            .OrderBy(w => w.Number)
            .Select(w => new WarehouseDto(w.Id, w.Number))
            .ToListAsync(ct);

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken ct = default)
    {
        var number = (request.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
            throw new ValidationException(nameof(request.Number), "Номер склада обязателен.");

        if (await _db.Warehouses.AnyAsync(w => w.Number.ToLower() == number.ToLower(), ct))
            throw new ConflictException($"Склад «{number}» уже существует.");

        var now = _clock.GetUtcNow();
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Number = number, CreatedAt = now, UpdatedAt = now };
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto(warehouse.Id, warehouse.Number);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken ct = default)
    {
        var number = (request.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
            throw new ValidationException(nameof(request.Number), "Номер склада обязателен.");

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct)
                        ?? throw NotFoundException.For("Склад", id);

        if (await _db.Warehouses.AnyAsync(w => w.Id != id && w.Number.ToLower() == number.ToLower(), ct))
            throw new ConflictException($"Склад «{number}» уже существует.");

        warehouse.Number = number;
        warehouse.UpdatedAt = _clock.GetUtcNow();
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto(warehouse.Id, warehouse.Number);
    }
}
