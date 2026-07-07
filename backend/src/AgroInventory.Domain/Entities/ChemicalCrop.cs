namespace AgroInventory.Domain.Entities;

/// <summary>Связь химии и культур many-to-many (ТЗ §7.3). Ключ — пара chemical_id + crop_id.</summary>
public class ChemicalCrop
{
    public Guid ChemicalId { get; set; }
    public InventoryItem Chemical { get; set; } = null!;

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;
}
