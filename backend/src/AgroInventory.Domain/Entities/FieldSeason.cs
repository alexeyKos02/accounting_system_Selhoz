namespace AgroInventory.Domain.Entities;

/// <summary>Сезон/период выращивания культуры на конкретном поле.</summary>
public class FieldSeason
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    public Guid FieldId { get; set; }
    public Field Field { get; set; } = null!;

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;

    public int Year { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
