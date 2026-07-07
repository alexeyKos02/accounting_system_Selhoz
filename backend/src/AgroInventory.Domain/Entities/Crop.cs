namespace AgroInventory.Domain.Entities;

/// <summary>Культура (ТЗ §7.3). В MVP — только название.</summary>
public class Crop
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ChemicalCrop> ChemicalCrops { get; set; } = new List<ChemicalCrop>();
}
