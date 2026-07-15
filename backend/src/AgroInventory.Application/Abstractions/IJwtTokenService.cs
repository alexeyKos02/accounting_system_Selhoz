using AgroInventory.Domain.Entities;

namespace AgroInventory.Application.Abstractions;

/// <summary>Выданный access-токен и его срок действия.</summary>
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>Сгенерированный refresh-токен: «сырое» значение клиенту, хэш — в БД.</summary>
public sealed record RefreshTokenResult(string RawToken, string TokenHash, DateTimeOffset ExpiresAt);

/// <summary>
/// Выпуск JWT access-токенов и refresh-токенов (ТЗ §1). Вся крипто-логика (подпись, хэш) —
/// в Infrastructure; ключи и сроки берутся из конфигурации (секция Jwt).
/// </summary>
public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(User user);

    RefreshTokenResult CreateRefreshToken();

    /// <summary>SHA-256 хэш «сырого» refresh-токена для поиска в БД (refresh/logout).</summary>
    string HashRefreshToken(string rawToken);
}
