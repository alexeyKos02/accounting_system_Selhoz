using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Членство пользователя в хозяйстве (ТЗ §3). Таблица `company_memberships`.
/// Один пользователь может иметь несколько членств с разными ролями в разных хозяйствах.
/// При удалении из хозяйства аккаунт не удаляется — деактивируется только доступ (Status = Removed).
/// </summary>
public class CompanyMembership
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Роль в этом хозяйстве (ТЗ §4). SystemAdmin здесь не используется — это глобальная роль.</summary>
    public AppRole Role { get; set; }

    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MembershipAccessScope> AccessScopes { get; set; } = new List<MembershipAccessScope>();
}
