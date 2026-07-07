namespace AgroInventory.Domain.Constants;

/// <summary>
/// Фиксированные идентификаторы для seed-данных (ТЗ §6).
/// Все операции и audit log в MVP пишутся от системного пользователя.
/// </summary>
public static class SystemIds
{
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid AdminRoleId = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid AppSettingsId = new("00000000-0000-0000-0000-000000000003");

    /// <summary>Фиксированные ID прав (по порядку Permissions.All).</summary>
    public static readonly IReadOnlyDictionary<string, Guid> PermissionIds = new Dictionary<string, Guid>
    {
        [Permissions.ManageSettings]  = new("00000000-0000-0000-0000-000000000101"),
        [Permissions.RestoreBackup]   = new("00000000-0000-0000-0000-000000000102"),
        [Permissions.ManageUsers]     = new("00000000-0000-0000-0000-000000000103"),
        [Permissions.ManageInventory] = new("00000000-0000-0000-0000-000000000104"),
        [Permissions.ViewAuditLog]    = new("00000000-0000-0000-0000-000000000105"),
    };

    /// <summary>Фиксированная дата для seed (HasData требует статичных значений).</summary>
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
