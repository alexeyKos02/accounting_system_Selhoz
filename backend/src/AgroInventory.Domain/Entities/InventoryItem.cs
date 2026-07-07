using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Общая сущность складского объекта (ТЗ §7.1). В MVP используется только ItemType.Chemical.
/// Система строится вокруг inventory_items, а не изолированной таблицы chemicals.
/// </summary>
public class InventoryItem
{
    public Guid Id { get; set; }
    public ItemType ItemType { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemStatus Status { get; set; } = ItemStatus.Active;

    /// <summary>Для merged-карточек — ссылка на основную карточку (ТЗ §18.2).</summary>
    public Guid? MergedIntoItemId { get; set; }
    public InventoryItem? MergedIntoItem { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Навигация
    public ChemicalDetails? ChemicalDetails { get; set; }
    public ICollection<ChemicalCrop> ChemicalCrops { get; set; } = new List<ChemicalCrop>();
}
