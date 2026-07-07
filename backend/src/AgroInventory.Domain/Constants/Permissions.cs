namespace AgroInventory.Domain.Constants;

/// <summary>Коды прав доступа (ТЗ §6). Заложены на будущее; в MVP авторизации нет.</summary>
public static class Permissions
{
    public const string ManageSettings = "manage_settings";
    public const string RestoreBackup = "restore_backup";
    public const string ManageUsers = "manage_users";
    public const string ManageInventory = "manage_inventory";
    public const string ViewAuditLog = "view_audit_log";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ManageSettings, RestoreBackup, ManageUsers, ManageInventory, ViewAuditLog,
    };
}
