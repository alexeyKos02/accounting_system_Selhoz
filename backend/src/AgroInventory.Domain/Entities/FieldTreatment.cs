namespace AgroInventory.Domain.Entities;

/// <summary>Обработка поля химией. Складское списание создаётся отдельным InventoryMovement.</summary>
public class FieldTreatment
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    public Guid FieldId { get; set; }
    public Field Field { get; set; } = null!;

    public Guid ChemicalId { get; set; }
    public InventoryItem Chemical { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;

    public Guid MovementId { get; set; }
    public InventoryMovement Movement { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal? RatePerHectare { get; set; }
    public DateTimeOffset TreatedAt { get; set; }
    public string? Comment { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
