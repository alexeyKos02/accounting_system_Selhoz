using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Область доступа членства (ТЗ §6). Таблица `membership_access_scopes`.
/// Если есть scope типа Company — доступ ко всему хозяйству. Иначе доступ ограничен
/// перечисленными складами (ScopeEntityId → warehouse) и/или полями (ScopeEntityId → field).
/// </summary>
public class MembershipAccessScope
{
    public Guid Id { get; set; }

    public Guid MembershipId { get; set; }
    public CompanyMembership Membership { get; set; } = null!;

    public AccessScopeType ScopeType { get; set; }

    /// <summary>Ссылка на склад/поле. Для ScopeType.Company не заполняется (null).</summary>
    public Guid? ScopeEntityId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
