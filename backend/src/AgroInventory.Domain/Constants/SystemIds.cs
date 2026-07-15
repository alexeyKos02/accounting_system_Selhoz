namespace AgroInventory.Domain.Constants;

/// <summary>
/// Фиксированные идентификаторы seed-данных.
/// Системные операции и audit log пишутся от системного пользователя (IsSystemAdmin).
/// Дефолтное хозяйство — для greenfield-старта и как контекст записи до полноценной авторизации.
/// </summary>
public static class SystemIds
{
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid AppSettingsId = new("00000000-0000-0000-0000-000000000003");

    /// <summary>Дефолтное хозяйство (ТЗ §25): к нему привязываются справочники/операции по умолчанию.</summary>
    public static readonly Guid DefaultCompanyId = new("00000000-0000-0000-0000-000000000201");

    /// <summary>Членство системного пользователя в дефолтном хозяйстве (роль Owner).</summary>
    public static readonly Guid SystemMembershipId = new("00000000-0000-0000-0000-000000000202");

    /// <summary>Область доступа членства — на всё хозяйство (scope_type = company).</summary>
    public static readonly Guid SystemMembershipScopeId = new("00000000-0000-0000-0000-000000000203");

    /// <summary>Фиксированная дата для seed (HasData требует статичных значений).</summary>
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
