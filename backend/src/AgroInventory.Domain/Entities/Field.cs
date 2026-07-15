namespace AgroInventory.Domain.Entities;

/// <summary>Поле/участок, на которое вносят химию (справочник). В MVP — только номер.</summary>
public class Field
{
    public Guid Id { get; set; }

    /// <summary>Хозяйство-владелец поля (ТЗ §7). Поле принадлежит одной компании/хозяйству.</summary>
    public Guid CompanyId { get; set; }

    public string Number { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
