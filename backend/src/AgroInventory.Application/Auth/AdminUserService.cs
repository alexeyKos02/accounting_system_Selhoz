using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Auth;

/// <summary>
/// Управление аккаунтами системным администратором (ТЗ §1, §21). Создаёт пользователя с
/// временным паролем (must_change_password), блокирует, сбрасывает пароль. Назначение хозяйств,
/// ролей и областей доступа — отдельные endpoints членств (этап C).
/// </summary>
public sealed class AdminUserService
{
    private const string EntityType = "User";

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public AdminUserService(
        IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenService tokens,
        IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken ct = default) =>
        await _db.Users.Where(u => !u.IsSystem)
            .OrderBy(u => u.Email)
            .Select(u => new AdminUserDto(
                u.Id, u.Email, u.FirstName, u.LastName, u.Phone,
                u.Status, u.IsSystemAdmin, u.MustChangePassword, u.CreatedAt))
            .ToListAsync(ct);

    public async Task<AdminUserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            throw new ValidationException(nameof(request.Email), "Укажите корректный e-mail.");
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ValidationException(nameof(request.FirstName), "Имя обязательно.");
        AuthService.ValidatePassword(request.Password, nameof(request.Password));

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"Пользователь с e-mail «{email}» уже существует.");

        var now = _clock.GetUtcNow();
        var firstName = request.FirstName.Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            FirstName = firstName,
            LastName = lastName,
            Phone = Trim(request.Phone),
            DisplayName = BuildDisplayName(firstName, lastName, email),
            Status = UserStatus.Active,
            MustChangePassword = true, // временный пароль меняется при первом входе (ТЗ §1)
            IsSystemAdmin = request.IsSystemAdmin,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Users.Add(user);

        _audit.Log(AuditAction.Create, EntityType, user.Id,
            null, new { user.Email, user.FirstName, user.LastName, user.IsSystemAdmin });
        await _db.SaveChangesAsync(ct);

        return ToDto(user);
    }

    public async Task<AdminUserDto> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await GetEditableAsync(userId, ct);
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ValidationException(nameof(request.FirstName), "Имя обязательно.");

        var old = new { user.FirstName, user.LastName, user.Phone, user.IsSystemAdmin };

        user.FirstName = request.FirstName.Trim();
        user.LastName = (request.LastName ?? string.Empty).Trim();
        user.Phone = Trim(request.Phone);
        user.IsSystemAdmin = request.IsSystemAdmin;
        user.DisplayName = BuildDisplayName(user.FirstName, user.LastName, user.Email);
        user.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Update, EntityType, user.Id,
            old, new { user.FirstName, user.LastName, user.Phone, user.IsSystemAdmin });
        await _db.SaveChangesAsync(ct);

        return ToDto(user);
    }

    /// <summary>Блокировка/разблокировка (ТЗ §1). При блокировке — отзыв всех refresh-токенов.</summary>
    public async Task<AdminUserDto> SetBlockedAsync(Guid userId, bool blocked, CancellationToken ct = default)
    {
        var user = await GetEditableAsync(userId, ct);
        var now = _clock.GetUtcNow();
        var old = new { user.Status };

        user.Status = blocked ? UserStatus.Blocked : UserStatus.Active;
        user.UpdatedAt = now;

        if (blocked)
        {
            var active = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct);
            foreach (var t in active) t.RevokedAt = now;
        }

        _audit.Log(AuditAction.Update, EntityType, user.Id, old, new { user.Status });
        await _db.SaveChangesAsync(ct);

        return ToDto(user);
    }

    /// <summary>Сброс пароля на новый временный (ТЗ §21). Ставит must_change_password и отзывает сессии.</summary>
    public async Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await GetEditableAsync(userId, ct);
        AuthService.ValidatePassword(request.NewPassword, nameof(request.NewPassword));

        var now = _clock.GetUtcNow();
        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = now;

        var active = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = now;

        _audit.Log(AuditAction.Update, EntityType, user.Id, null, new { PasswordReset = true });
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Помощники ----------

    private async Task<User> GetEditableAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw NotFoundException.For("Пользователь", userId);
        if (user.IsSystem)
            throw new ConflictException("Системный пользователь не редактируется.");
        return user;
    }

    private static AdminUserDto ToDto(User u) => new(
        u.Id, u.Email, u.FirstName, u.LastName, u.Phone,
        u.Status, u.IsSystemAdmin, u.MustChangePassword, u.CreatedAt);

    private static string BuildDisplayName(string firstName, string lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return name.Length > 0 ? name : (email ?? string.Empty);
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
