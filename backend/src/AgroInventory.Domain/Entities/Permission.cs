namespace AgroInventory.Domain.Entities;

/// <summary>Право доступа (ТЗ §6). Коды — в Domain.Constants.Permissions.</summary>
public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
