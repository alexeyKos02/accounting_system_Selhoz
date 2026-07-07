namespace AgroInventory.Domain.Enums;

/// <summary>Тип складского движения (ТЗ §9). Transfer заложен, но в MVP не реализуется.</summary>
public enum MovementType
{
    Income = 1,
    Outcome = 2,
    Correction = 3,
    Transfer = 4,
}
