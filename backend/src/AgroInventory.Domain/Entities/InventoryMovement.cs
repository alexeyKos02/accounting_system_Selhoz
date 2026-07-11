using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Складское движение — источник истины по остаткам (ТЗ §9.1, §31.1).
/// Мягкое удаление (is_deleted) — операция остаётся для аудита (ТЗ §20).
/// </summary>
public class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid ChemicalId { get; set; }
    public InventoryItem Chemical { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public MovementType MovementType { get; set; }

    /// <summary>Величина операции в литрах (для income/outcome/correction).</summary>
    public decimal QuantityLiters { get; set; }

    public UnitType? UnitType { get; set; }
    public decimal? PackageVolumeLiters { get; set; }
    public int? PackagesQuantity { get; set; }

    public Guid? CropId { get; set; }
    public Crop? Crop { get; set; }

    /// <summary>Поле/участок — для списания (необязательно, ТЗ §11).</summary>
    public Guid? FieldId { get; set; }
    public Field? Field { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
    public string? Comment { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<InventoryMovementDetail> Details { get; set; } = new List<InventoryMovementDetail>();
}
