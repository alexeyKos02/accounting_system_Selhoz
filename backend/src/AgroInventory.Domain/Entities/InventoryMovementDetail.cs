using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Детали операции для сложных списаний из нескольких источников (ТЗ §9.2).
/// Пример списания 5 л: 2 л из вскрытой банки + 3 л из loose_liters.
/// </summary>
public class InventoryMovementDetail
{
    public Guid Id { get; set; }
    public Guid MovementId { get; set; }
    public InventoryMovement Movement { get; set; } = null!;

    public MovementSourceType SourceType { get; set; }

    /// <summary>Ссылка на конкретный источник (package_group/opened_package), если применимо.</summary>
    public Guid? SourceId { get; set; }

    public UnitType? UnitType { get; set; }
    public decimal? PackageVolumeLiters { get; set; }
    public decimal QuantityLiters { get; set; }
    public int? PackagesQuantity { get; set; }
}
