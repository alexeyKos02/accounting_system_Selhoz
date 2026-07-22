namespace AgroInventory.Domain.Enums;

/// <summary>
/// Роль (ТЗ §4). Фиксированный набор v1. SystemAdmin — глобальная роль на уровне пользователя
/// (User.IsSystemAdmin), остальные — роль в рамках конкретного членства (CompanyMembership.Role).
/// Права роли — статическая карта в <see cref="Constants.RolePermissionMap"/>.
/// </summary>
public enum AppRole
{
    SystemAdmin = 0,
    Owner = 1,
    CompanyAdmin = 2,
    Manager = 3,
    Storekeeper = 4,
    Viewer = 5,

    // Роли добавлены в конец, чтобы не сдвигать уже сохранённые в БД значения (миграция не нужна).
    /// <summary>Агроном: поля/сезоны/обработки/урожай. Не ведёт приход на склад.</summary>
    Agronomist = 6,
    /// <summary>Учётчик/ревизор: только просмотр по всему + отчёты и аудит, без права менять.</summary>
    Auditor = 7,
}
