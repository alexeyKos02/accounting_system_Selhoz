namespace AgroInventory.Domain.Enums;

/// <summary>Статус складского объекта (ТЗ §7.1, §17, §18).</summary>
public enum ItemStatus
{
    Active = 1,
    Archived = 2,
    Merged = 3,
}
