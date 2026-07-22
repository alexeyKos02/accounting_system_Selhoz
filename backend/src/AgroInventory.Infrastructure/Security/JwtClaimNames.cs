namespace AgroInventory.Infrastructure.Security;

/// <summary>Имена claim'ов в access-токене. MapInboundClaims выключен — читаем как есть.</summary>
public static class JwtClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string IsSystemAdmin = "is_system_admin";
    public const string CanAddToCatalog = "can_add_to_catalog";
    public const string Name = "name";
}
