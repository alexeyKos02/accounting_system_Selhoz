using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Пользователь (ТЗ §1). Таблица `users`. Аутентификация (пароль/JWT/Identity) подключается на этапе B —
/// поля PasswordHash/Email заложены здесь и пока могут быть пустыми у системного пользователя.
/// Роли per-company хранятся в <see cref="CompanyMembership"/>; SystemAdmin — глобальный флаг.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>E-mail — логин пользователя (ТЗ §1). У системного пользователя может отсутствовать.</summary>
    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>Требуется смена временного пароля при первом входе (ТЗ §1).</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Глобальный системный администратор всего AgroInventory (ТЗ §4).</summary>
    public bool IsSystemAdmin { get; set; }

    /// <summary>
    /// Право добавлять препараты в общий канонический каталог (§12). Глобальный флаг, по образцу
    /// <see cref="IsSystemAdmin"/>. Разрешает только создание записей каталога (не редактирование —
    /// это по-прежнему только SystemAdmin). SystemAdmin имеет это право неявно.
    /// </summary>
    public bool CanAddToCatalog { get; set; }

    /// <summary>Служебный пользователь для системных записей/аудита (не логинится).</summary>
    public bool IsSystem { get; set; }

    /// <summary>Отображаемое имя для аудита/списков (обычно «Имя Фамилия»).</summary>
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CompanyMembership> Memberships { get; set; } = new List<CompanyMembership>();
}
