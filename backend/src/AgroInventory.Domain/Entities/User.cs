namespace AgroInventory.Domain.Entities;

/// <summary>
/// Пользователь (ТЗ §6). В MVP авторизации нет — все операции пишутся от системного
/// пользователя (IsSystem = true). Сущность заложена под будущую авторизацию.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
