namespace AgroInventory.Domain.Entities;

/// <summary>
/// Refresh-токен (ТЗ §1). Таблица `refresh_tokens`. Храним только хэш токена (SHA-256),
/// сам токен отдаётся клиенту один раз. Ротация: при обновлении старый помечается Revoked.
/// «Выход со всех устройств» — отзыв всех активных токенов пользователя.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 хэш токена (Base64). Сам токен в БД не хранится.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Момент отзыва (logout / ротация / выход со всех устройств). NULL — активен.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
