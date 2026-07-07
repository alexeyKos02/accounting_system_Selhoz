namespace AgroInventory.Domain.Entities;

/// <summary>Детали только для химии (ТЗ §7.2). Срок годности/партия/цена в MVP не нужны.</summary>
public class ChemicalDetails
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;

    public string? Manufacturer { get; set; }
    public string? Comment { get; set; }
}
