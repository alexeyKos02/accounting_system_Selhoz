namespace AgroInventory.Domain.Entities;

/// <summary>Поле/участок, на которое вносят химию (справочник). В MVP — только номер.</summary>
public class Field
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
