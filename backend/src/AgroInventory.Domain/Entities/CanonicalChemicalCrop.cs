namespace AgroInventory.Domain.Entities;

/// <summary>
/// Связь канонического препарата и культур many-to-many (ТЗ §12, аналог §7.3 для карточки химии).
/// Ключ — пара canonical_chemical_id + crop_id. Таблица `canonical_chemical_crops`.
/// </summary>
public class CanonicalChemicalCrop
{
    public Guid CanonicalChemicalId { get; set; }
    public CanonicalChemical CanonicalChemical { get; set; } = null!;

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;
}
