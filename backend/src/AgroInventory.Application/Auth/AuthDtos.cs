using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Auth;

// ---------- Аутентификация (ТЗ §1, §21) ----------

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Пара токенов + признак обязательной смены временного пароля (ТЗ §1).</summary>
public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    bool MustChangePassword);

/// <summary>Данные текущего пользователя (GET /api/users/me, ТЗ §21).</summary>
public sealed record MeResponse(
    Guid Id,
    string? Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsSystemAdmin,
    bool MustChangePassword,
    IReadOnlyList<MembershipInfo> Memberships);

/// <summary>Членство пользователя в хозяйстве с вычисленными правами роли (ТЗ §4, §5).</summary>
public sealed record MembershipInfo(
    Guid CompanyId,
    string CompanyName,
    AppRole Role,
    MembershipStatus Status,
    IReadOnlyList<string> Permissions);

// ---------- Администрирование аккаунтов (ТЗ §21) ----------

public sealed record AdminUserDto(
    Guid Id,
    string? Email,
    string FirstName,
    string LastName,
    string? Phone,
    UserStatus Status,
    bool IsSystemAdmin,
    bool MustChangePassword,
    DateTimeOffset CreatedAt);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsSystemAdmin);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? Phone,
    bool IsSystemAdmin);

public sealed record ResetPasswordRequest(string NewPassword);
