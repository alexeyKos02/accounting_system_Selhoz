using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Вскрытая упаковка (ТЗ §8.4). Хранится отдельно. Когда remaining_liters = 0 —
/// упаковка удаляется из активных остатков (пустые не храним), история остаётся в movements.
/// </summary>
public class OpenedPackage
{
    public Guid Id { get; set; }
    public Guid ChemicalId { get; set; }
    public InventoryItem Chemical { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public UnitType UnitType { get; set; }
    public decimal InitialLiters { get; set; }
    public decimal RemainingLiters { get; set; }

    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
