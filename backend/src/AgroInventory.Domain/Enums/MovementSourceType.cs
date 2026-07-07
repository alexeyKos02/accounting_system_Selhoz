namespace AgroInventory.Domain.Enums;

/// <summary>Источник, из которого списаны литры в деталях операции (ТЗ §9.2).</summary>
public enum MovementSourceType
{
    LooseLiters = 1,
    PackageGroup = 2,
    OpenedPackage = 3,
}
