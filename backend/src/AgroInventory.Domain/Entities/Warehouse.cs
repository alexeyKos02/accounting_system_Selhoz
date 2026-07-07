namespace AgroInventory.Domain.Entities;

/// <summary>Склад (ТЗ §7.4). В MVP — это номер. Название/адрес/ответственный — на будущее.</summary>
public class Warehouse
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
