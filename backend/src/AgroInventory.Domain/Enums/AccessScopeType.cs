namespace AgroInventory.Domain.Enums;

/// <summary>
/// Тип области доступа членства (ТЗ §6). Если у членства есть scope типа Company —
/// доступ ко всему хозяйству; иначе доступ только к перечисленным складам/полям.
/// </summary>
public enum AccessScopeType
{
    Company = 0,
    Warehouse = 1,
    Field = 2,
}
