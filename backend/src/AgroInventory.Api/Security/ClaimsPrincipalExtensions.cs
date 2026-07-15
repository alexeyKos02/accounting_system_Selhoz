using System.Security.Claims;
using AgroInventory.Application.Common;
using AgroInventory.Infrastructure.Security;

namespace AgroInventory.Api.Security;

/// <summary>Имена политик авторизации.</summary>
public static class AuthorizationPolicies
{
    public const string SystemAdmin = "SystemAdmin";
}

/// <summary>Извлечение идентификатора пользователя из claim'ов access-токена.</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(JwtClaimNames.Subject)?.Value;
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("Не удалось определить пользователя.");
    }
}
