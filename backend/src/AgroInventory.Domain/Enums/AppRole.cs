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
}
