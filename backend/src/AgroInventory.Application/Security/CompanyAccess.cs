using AgroInventory.Application.Common;
using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Security;

/// <summary>
/// Доступное пользователю хозяйство с областью доступа по складам (ТЗ §15, §17). HasFullScope —
/// доступ ко всем складам хозяйства; иначе доступ ограничен WarehouseIds.
/// </summary>
public sealed record AccessibleCompany(
    Guid CompanyId, string Name, bool HasFullScope, IReadOnlySet<Guid> WarehouseIds);

/// <summary>
/// Разрешённый доступ пользователя к выбранному хозяйству (ТЗ §4–§6, §24): роль, права,
/// область доступа по складам/полям. Для SystemAdmin — полный доступ ко всему хозяйству.
/// </summary>
public sealed class CompanyAccess
{
    public required Guid CompanyId { get; init; }
    public required AppRole Role { get; init; }
    public required bool IsSystemAdmin { get; init; }
    public required IReadOnlySet<string> Permissions { get; init; }

    /// <summary>Доступ ко всему хозяйству (есть scope типа Company либо SystemAdmin).</summary>
    public required bool HasFullScope { get; init; }

    /// <summary>Разрешённые склады, когда доступ ограничен (HasFullScope = false).</summary>
    public required IReadOnlySet<Guid> WarehouseIds { get; init; }

    /// <summary>Разрешённые поля, когда доступ ограничен (HasFullScope = false).</summary>
    public required IReadOnlySet<Guid> FieldIds { get; init; }

    public bool Has(string permission) => Permissions.Contains(permission);

    public void Require(string permission)
    {
        if (!Has(permission))
            throw new ForbiddenException("Недостаточно прав для выполнения действия.");
    }

    public bool CanAccessWarehouse(Guid warehouseId) => HasFullScope || WarehouseIds.Contains(warehouseId);

    public void RequireWarehouse(Guid warehouseId)
    {
        if (!CanAccessWarehouse(warehouseId))
            throw new ForbiddenException("Нет доступа к выбранному складу.");
    }

    public bool CanAccessField(Guid fieldId) => HasFullScope || FieldIds.Contains(fieldId);

    public void RequireField(Guid fieldId)
    {
        if (!CanAccessField(fieldId))
            throw new ForbiddenException("Нет доступа к выбранному полю.");
    }
}
