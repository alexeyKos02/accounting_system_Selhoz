using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Auth;

/// <summary>
/// Аутентификация (ТЗ §1, §21): вход, обновление токенов, выход, выход со всех устройств,
/// смена пароля, данные текущего пользователя. Пароли хранятся хэшами (IPasswordHasher).
/// </summary>
public sealed class AuthService
{
    /// <summary>Минимальная длина пароля.</summary>
    public const int MinPasswordLength = 8;

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly TimeProvider _clock;

    public AuthService(IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenService tokens, TimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
    }

    // ---------- Вход ----------

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsSystem, ct);

        // Единое сообщение для «нет пользователя» и «неверный пароль» — не раскрываем, что e-mail существует.
        if (user is null || user.PasswordHash is null || !_hasher.Verify(user.PasswordHash, request.Password ?? string.Empty))
            throw new UnauthorizedException("Неверный e-mail или пароль.");

        if (user.Status == UserStatus.Blocked)
            throw new UnauthorizedException("Пользователь заблокирован. Обратитесь к администратору.");
        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException("Неверный e-mail или пароль.");

        return await IssueTokensAsync(user, ct);
    }

    // ---------- Обновление токенов (ротация) ----------

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken ?? string.Empty);
        var now = _clock.GetUtcNow();

        var token = await _db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || !token.IsActive(now))
            throw new UnauthorizedException("Сессия недействительна. Войдите заново.");

        if (token.User.Status != UserStatus.Active)
            throw new UnauthorizedException("Пользователь недоступен. Войдите заново.");

        // Ротация: старый токен отзываем, выдаём новую пару.
        token.RevokedAt = now;
        return await IssueTokensAsync(token.User, ct);
    }

    // ---------- Выход ----------

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken ?? string.Empty);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is { RevokedAt: null })
        {
            token.RevokedAt = _clock.GetUtcNow();
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Выход со всех устройств (ТЗ §1): отзыв всех активных refresh-токенов пользователя.</summary>
    public async Task LogoutAllAsync(Guid userId, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var active = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = now;
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Смена пароля ----------

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException("Пользователь не найден.");

        if (user.PasswordHash is null || !_hasher.Verify(user.PasswordHash, request.CurrentPassword ?? string.Empty))
            throw new ValidationException(nameof(request.CurrentPassword), "Текущий пароль неверен.");

        ValidatePassword(request.NewPassword, nameof(request.NewPassword));

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = _clock.GetUtcNow();
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Текущий пользователь ----------

    public async Task<MeResponse> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException("Пользователь не найден.");

        var memberships = await _db.CompanyMemberships
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .Select(m => new { m.CompanyId, CompanyName = m.Company.Name, m.Role, m.Status })
            .ToListAsync(ct);

        var membershipInfos = memberships
            .Select(m => new MembershipInfo(
                m.CompanyId, m.CompanyName, m.Role, m.Status,
                RolePermissionMap.Map.TryGetValue(m.Role, out var perms) ? perms.ToList() : new List<string>()))
            .ToList();

        return new MeResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.Phone,
            user.IsSystemAdmin, user.CanAddToCatalog, user.MustChangePassword, membershipInfos);
    }

    // ---------- Помощники ----------

    private async Task<TokenResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAt,
            CreatedAt = _clock.GetUtcNow(),
        });
        await _db.SaveChangesAsync(ct);

        return new TokenResponse(access.Token, access.ExpiresAt, refresh.RawToken, user.MustChangePassword);
    }

    internal static void ValidatePassword(string? password, string field)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ValidationException(field, $"Пароль должен быть не короче {MinPasswordLength} символов.");
    }
}
