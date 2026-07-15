using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Companies;

/// <summary>
/// Членства пользователей в хозяйстве и области доступа (ТЗ §3, §6, §21). Требует права users.manage
/// в этом хозяйстве. Удаление члена = деактивация доступа (Removed), аккаунт не удаляется (ТЗ §3).
/// Нельзя оставить хозяйство без активного Owner (ограничение CompanyAdmin, ТЗ §4).
/// </summary>
public sealed class MembershipService
{
    private const string EntityType = "CompanyMembership";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public MembershipService(
        IApplicationDbContext db, ICurrentUser currentUser, CompanyContextService companyContext,
        IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<MemberDto>> ListAsync(Guid companyId, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireForCompanyAsync(companyId, ct);
        access.Require(Permissions.UsersView);

        return await _db.CompanyMemberships
            .Where(m => m.CompanyId == companyId && m.Status != MembershipStatus.Removed)
            .OrderBy(m => m.User.Email)
            .Select(m => new MemberDto(
                m.Id, m.UserId, m.User.Email, m.User.DisplayName, m.Role, m.Status))
            .ToListAsync(ct);
    }

    /// <summary>Добавляет пользователя в хозяйство с ролью (ТЗ §3). По умолчанию — доступ ко всему хозяйству.</summary>
    public async Task<MemberDto> AddAsync(Guid companyId, AddMemberRequest request, CancellationToken ct = default)
    {
        await RequireManageAsync(companyId, ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsSystem, ct)
                   ?? throw NotFoundException.For("Пользователь", request.UserId);

        var existing = await _db.CompanyMemberships
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == request.UserId, ct);

        var now = _clock.GetUtcNow();
        Guid membershipId;

        if (existing is not null)
        {
            if (existing.Status != MembershipStatus.Removed)
                throw new ConflictException("Пользователь уже состоит в хозяйстве.");

            // Реактивация ранее удалённого членства (ТЗ §3).
            existing.Role = request.Role;
            existing.Status = MembershipStatus.Active;
            existing.UpdatedAt = now;
            membershipId = existing.Id;
        }
        else
        {
            membershipId = Guid.NewGuid();
            _db.CompanyMemberships.Add(new CompanyMembership
            {
                Id = membershipId,
                UserId = request.UserId,
                CompanyId = companyId,
                Role = request.Role,
                Status = MembershipStatus.Active,
                CreatedByUserId = _currentUser.UserId,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        // По умолчанию — доступ ко всему хозяйству; администратор может сузить через scopes (ТЗ §6).
        var hasAnyScope = await _db.MembershipAccessScopes.AnyAsync(s => s.MembershipId == membershipId, ct);
        if (!hasAnyScope)
        {
            _db.MembershipAccessScopes.Add(new MembershipAccessScope
            {
                Id = Guid.NewGuid(),
                MembershipId = membershipId,
                ScopeType = AccessScopeType.Company,
                ScopeEntityId = null,
                CreatedAt = now,
            });
        }

        _audit.Log(AuditAction.Create, EntityType, membershipId,
            null, new { companyId, request.UserId, request.Role });
        await _db.SaveChangesAsync(ct);

        return new MemberDto(membershipId, user.Id, user.Email, user.DisplayName, request.Role, MembershipStatus.Active);
    }

    public async Task<MemberDto> UpdateAsync(
        Guid companyId, Guid membershipId, UpdateMemberRequest request, CancellationToken ct = default)
    {
        await RequireManageAsync(companyId, ct);
        var membership = await GetMembershipAsync(companyId, membershipId, ct);

        // Нельзя оставить хозяйство без активного Owner (ТЗ §4).
        var losesOwner = membership.Role == AppRole.Owner
                         && (request.Role != AppRole.Owner || request.Status != MembershipStatus.Active);
        if (losesOwner)
            await EnsureNotLastOwnerAsync(companyId, membershipId, ct);

        var old = new { membership.Role, membership.Status };
        membership.Role = request.Role;
        membership.Status = request.Status;
        membership.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Update, EntityType, membership.Id, old, new { membership.Role, membership.Status });
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FirstAsync(u => u.Id == membership.UserId, ct);
        return new MemberDto(membership.Id, user.Id, user.Email, user.DisplayName, membership.Role, membership.Status);
    }

    /// <summary>Удаление члена = деактивация доступа (Status = Removed), аккаунт остаётся (ТЗ §3).</summary>
    public async Task RemoveAsync(Guid companyId, Guid membershipId, CancellationToken ct = default)
    {
        await RequireManageAsync(companyId, ct);
        var membership = await GetMembershipAsync(companyId, membershipId, ct);

        if (membership.Role == AppRole.Owner)
            await EnsureNotLastOwnerAsync(companyId, membershipId, ct);

        membership.Status = MembershipStatus.Removed;
        membership.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Delete, EntityType, membership.Id, null, new { Removed = true });
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Области доступа (ТЗ §6) ----------

    public async Task<MemberScopesDto> GetScopesAsync(Guid companyId, Guid membershipId, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireForCompanyAsync(companyId, ct);
        access.Require(Permissions.UsersView);
        await GetMembershipAsync(companyId, membershipId, ct);

        var scopes = await _db.MembershipAccessScopes
            .Where(s => s.MembershipId == membershipId)
            .Select(s => new ScopeItemDto(s.ScopeType, s.ScopeEntityId))
            .ToListAsync(ct);

        var hasFull = scopes.Any(s => s.ScopeType == AccessScopeType.Company);
        return new MemberScopesDto(hasFull, scopes);
    }

    /// <summary>Заменяет области доступа членства (ТЗ §6). Company-scope даёт доступ ко всему хозяйству.</summary>
    public async Task<MemberScopesDto> UpdateScopesAsync(
        Guid companyId, Guid membershipId, UpdateScopesRequest request, CancellationToken ct = default)
    {
        await RequireManageAsync(companyId, ct);
        await GetMembershipAsync(companyId, membershipId, ct);

        var now = _clock.GetUtcNow();
        var hasCompanyScope = request.Scopes.Any(s => s.ScopeType == AccessScopeType.Company);

        var newScopes = new List<MembershipAccessScope>();
        if (hasCompanyScope)
        {
            // Доступ ко всему хозяйству — одна запись типа Company, остальное игнорируем (ТЗ §6).
            newScopes.Add(NewScope(membershipId, AccessScopeType.Company, null, now));
        }
        else
        {
            var warehouseIds = request.Scopes
                .Where(s => s.ScopeType == AccessScopeType.Warehouse && s.ScopeEntityId is not null)
                .Select(s => s.ScopeEntityId!.Value).Distinct().ToList();
            var fieldIds = request.Scopes
                .Where(s => s.ScopeType == AccessScopeType.Field && s.ScopeEntityId is not null)
                .Select(s => s.ScopeEntityId!.Value).Distinct().ToList();

            await ValidateBelongToCompanyAsync(companyId, warehouseIds, fieldIds, ct);

            newScopes.AddRange(warehouseIds.Select(id => NewScope(membershipId, AccessScopeType.Warehouse, id, now)));
            newScopes.AddRange(fieldIds.Select(id => NewScope(membershipId, AccessScopeType.Field, id, now)));
        }

        var existing = await _db.MembershipAccessScopes.Where(s => s.MembershipId == membershipId).ToListAsync(ct);
        _db.MembershipAccessScopes.RemoveRange(existing);
        _db.MembershipAccessScopes.AddRange(newScopes);

        _audit.Log(AuditAction.Update, EntityType, membershipId,
            null, new { Scopes = newScopes.Select(s => new { s.ScopeType, s.ScopeEntityId }) });
        await _db.SaveChangesAsync(ct);

        return new MemberScopesDto(hasCompanyScope,
            newScopes.Select(s => new ScopeItemDto(s.ScopeType, s.ScopeEntityId)).ToList());
    }

    // ---------- Помощники ----------

    private async Task RequireManageAsync(Guid companyId, CancellationToken ct)
    {
        var access = await _companyContext.RequireForCompanyAsync(companyId, ct);
        access.Require(Permissions.UsersManage);
    }

    private async Task<CompanyMembership> GetMembershipAsync(Guid companyId, Guid membershipId, CancellationToken ct) =>
        await _db.CompanyMemberships.FirstOrDefaultAsync(m => m.Id == membershipId && m.CompanyId == companyId, ct)
        ?? throw NotFoundException.For("Членство", membershipId);

    private async Task EnsureNotLastOwnerAsync(Guid companyId, Guid membershipId, CancellationToken ct)
    {
        var otherOwners = await _db.CompanyMemberships.CountAsync(
            m => m.CompanyId == companyId
                 && m.Id != membershipId
                 && m.Role == AppRole.Owner
                 && m.Status == MembershipStatus.Active, ct);
        if (otherOwners == 0)
            throw new ConflictException("Нельзя убрать последнего владельца хозяйства.");
    }

    private async Task ValidateBelongToCompanyAsync(
        Guid companyId, List<Guid> warehouseIds, List<Guid> fieldIds, CancellationToken ct)
    {
        // IgnoreQueryFilters: проверяем принадлежность целевому хозяйству напрямую (глобальный фильтр
        // ориентируется на выбранное в заголовке хозяйство, а здесь companyId — из URL).
        if (warehouseIds.Count > 0)
        {
            var found = await _db.Warehouses.IgnoreQueryFilters()
                .CountAsync(w => w.CompanyId == companyId && warehouseIds.Contains(w.Id), ct);
            if (found != warehouseIds.Count)
                throw new ValidationException("scopes", "Некоторые склады не принадлежат этому хозяйству.");
        }
        if (fieldIds.Count > 0)
        {
            var found = await _db.Fields.IgnoreQueryFilters()
                .CountAsync(f => f.CompanyId == companyId && fieldIds.Contains(f.Id), ct);
            if (found != fieldIds.Count)
                throw new ValidationException("scopes", "Некоторые поля не принадлежат этому хозяйству.");
        }
    }

    private static MembershipAccessScope NewScope(Guid membershipId, AccessScopeType type, Guid? entityId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            MembershipId = membershipId,
            ScopeType = type,
            ScopeEntityId = entityId,
            CreatedAt = now,
        };
}
