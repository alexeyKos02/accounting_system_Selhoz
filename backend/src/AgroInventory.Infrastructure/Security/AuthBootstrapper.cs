using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Первичная инициализация доступа (ТЗ §25.2): если системного администратора-логина ещё нет,
/// создаёт его с временным паролем и членством Owner в дефолтном хозяйстве. Идемпотентно.
/// </summary>
public sealed class AuthBootstrapper
{
    private readonly AgroInventoryDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly AdminBootstrapOptions _options;
    private readonly TimeProvider _clock;

    public AuthBootstrapper(
        AgroInventoryDbContext db, IPasswordHasher hasher, AdminBootstrapOptions options, TimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _options = options;
        _clock = clock;
    }

    public async Task EnsureSeedAdminAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled) return;

        var email = _options.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct)) return;

        var now = _clock.GetUtcNow();
        var firstName = _options.FirstName.Trim();
        var lastName = _options.LastName.Trim();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(_options.Password),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}".Trim(),
            Status = UserStatus.Active,
            MustChangePassword = true, // сменить временный пароль при первом входе (ТЗ §1)
            IsSystemAdmin = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Users.Add(admin);

        // Членство Owner в дефолтном хозяйстве + доступ на всё хозяйство — чтобы вход был сразу
        // полезен (SystemAdmin и без членства видит всё, но контекст хозяйства удобно иметь).
        if (await _db.Companies.AnyAsync(c => c.Id == SystemIds.DefaultCompanyId, ct))
        {
            var membershipId = Guid.NewGuid();
            _db.CompanyMemberships.Add(new CompanyMembership
            {
                Id = membershipId,
                UserId = admin.Id,
                CompanyId = SystemIds.DefaultCompanyId,
                Role = AppRole.Owner,
                Status = MembershipStatus.Active,
                CreatedByUserId = SystemIds.SystemUserId,
                CreatedAt = now,
                UpdatedAt = now,
            });
            _db.MembershipAccessScopes.Add(new MembershipAccessScope
            {
                Id = Guid.NewGuid(),
                MembershipId = membershipId,
                ScopeType = AccessScopeType.Company,
                ScopeEntityId = null,
                CreatedAt = now,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
