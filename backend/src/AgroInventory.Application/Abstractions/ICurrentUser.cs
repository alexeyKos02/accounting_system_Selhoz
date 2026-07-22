namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Текущий пользователь. В MVP авторизации нет — всегда системный пользователь (ТЗ §6).
/// Абстракция заложена под будущую авторизацию: реализацию заменим, вызовы не изменятся.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    /// <summary>Глобальный системный администратор (ТЗ §4) — из claim'а токена.</summary>
    bool IsSystemAdmin { get; }

    /// <summary>Право добавлять препараты в общий каталог (§12) — из claim'а токена.</summary>
    bool CanAddToCatalog { get; }

    /// <summary>
    /// Явно выбранное хозяйство из HTTP-контекста (заголовок X-Company-Id, ТЗ §15).
    /// NULL — хозяйство не выбрано (режим «Все хозяйства» либо не-хозяйственный запрос).
    /// </summary>
    Guid? SelectedCompanyId { get; }

    /// <summary>
    /// Хозяйство-контекст для штампа company_id и глобальных query-фильтров: выбранное хозяйство,
    /// иначе дефолтное. Доступ к выбранному хозяйству валидируется в CompanyContextService (ТЗ §24).
    /// </summary>
    Guid CompanyId { get; }
}
