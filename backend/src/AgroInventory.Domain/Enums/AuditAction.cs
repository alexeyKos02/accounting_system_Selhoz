namespace AgroInventory.Domain.Enums;

/// <summary>Действие в журнале аудита (ТЗ §21.1).</summary>
public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Restore = 4,
    Archive = 5,
    Merge = 6,
}
