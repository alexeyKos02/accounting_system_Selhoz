using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Группа полных одинаковых упаковок (ТЗ §8.3): хранится одной строкой, чтобы не раздувать таблицу.
/// Пример: 100 банок по 10 л → unit_type=Can, package_volume_liters=10, quantity=100.
/// </summary>
public class PackageGroup
{
    public Guid Id { get; set; }
    public Guid ChemicalId { get; set; }
    public InventoryItem Chemical { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public UnitType UnitType { get; set; }
    public decimal PackageVolumeLiters { get; set; }
    public int Quantity { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
