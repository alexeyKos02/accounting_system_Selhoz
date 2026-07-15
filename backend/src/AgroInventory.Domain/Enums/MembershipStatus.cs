namespace AgroInventory.Domain.Enums;

/// <summary>Статус членства пользователя в хозяйстве (ТЗ §3).</summary>
public enum MembershipStatus
{
    Active = 0,
    Suspended = 1,
    Removed = 2,
}
