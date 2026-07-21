namespace AgroInventory.Domain.Entities;

/// <summary>Культура (ТЗ §7.3). В MVP — только название.</summary>
public class Crop
{
    public Guid Id { get; set; }

    /// <summary>
    /// Хозяйство-владелец культуры (ТЗ §8). NULL — системная культура, общая для всех хозяйств.
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Системная культура (общая для всех). Пользовательские принадлежат хозяйству.</summary>
    public bool IsSystem { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ChemicalCrop> ChemicalCrops { get; set; } = new List<ChemicalCrop>();
    public ICollection<CanonicalChemicalCrop> CanonicalChemicalCrops { get; set; } = new List<CanonicalChemicalCrop>();
}
