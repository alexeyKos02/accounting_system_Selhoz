namespace AgroInventory.Domain.Entities;

/// <summary>
/// Быстрый агрегированный остаток по паре химия+склад (ТЗ §8.2).
/// total_liters — для быстрых списков; источник истины по операциям — inventory_movements,
/// баланс можно пересчитать из истории.
/// </summary>
public class ChemicalStockBalance
{
    public Guid Id { get; set; }

    /// <summary>Хозяйство (ТЗ §13). Остаток хранится отдельно для каждого хозяйства и склада.</summary>
    public Guid CompanyId { get; set; }

    public Guid ChemicalId { get; set; }
    public InventoryItem Chemical { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Полный остаток в единице химии (литры/кг).</summary>
    public decimal TotalQuantity { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
