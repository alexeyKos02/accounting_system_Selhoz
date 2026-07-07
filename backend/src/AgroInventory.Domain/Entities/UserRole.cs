namespace AgroInventory.Domain.Entities;

/// <summary>Связь пользователь-роль (ТЗ §6).</summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
