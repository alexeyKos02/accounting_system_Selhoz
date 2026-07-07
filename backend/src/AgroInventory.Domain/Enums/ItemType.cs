namespace AgroInventory.Domain.Enums;

/// <summary>Тип складского объекта. В MVP используется только Chemical (ТЗ §7.1).</summary>
public enum ItemType
{
    Chemical = 1,
    SparePart = 2,
}
