namespace AgroInventory.Domain.Entities;

/// <summary>Роль (ТЗ §6). Заложена под будущую авторизацию.</summary>
public class Role
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
