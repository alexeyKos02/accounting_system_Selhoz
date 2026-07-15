using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Security;

/// <summary>
/// Разрешает и валидирует доступ текущего пользователя к выбранному хозяйству (ТЗ §24).
/// Scoped на запрос: результат кэшируется. Нельзя полагаться на company_id из тела/URL —
/// хозяйство берётся из заголовка X-Company-Id и проверяется по членствам пользователя.
/// </summary>
public sealed class CompanyContextService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _current;
    private CompanyAccess? _cached;

    public CompanyContextService(IApplicationDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    /// <summary>Проверяет доступ к выбранному (заголовок X-Company-Id) хозяйству. Результат кэшируется.</summary>
    public async Task<CompanyAccess> RequireAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return _cached;

        var companyId = _current.SelectedCompanyId
            ?? throw new ValidationException("company", "Не выбрано хозяйство (заголовок X-Company-Id).");

        _cached = await ResolveAsync(companyId, ct);
        return _cached;
    }

    /// <summary>
    /// Проверяет доступ к конкретному хозяйству по его id (не из заголовка) — для управления
    /// членствами/настройками хозяйства, где companyId берётся из URL (ТЗ §21).
    /// </summary>
    public Task<CompanyAccess> RequireForCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        ResolveAsync(companyId, ct);

    /// <summary>Проверяет доступ к выбранному хозяйству и наличие права (ТЗ §5).</summary>
    public async Task<CompanyAccess> RequirePermissionAsync(string permission, CancellationToken ct = default)
    {
        var access = await RequireAsync(ct);
        access.Require(permission);
        return access;
    }

    private async Task<CompanyAccess> ResolveAsync(Guid companyId, CancellationToken ct)
    {
        // Хозяйство должно существовать и быть активным (companies не под company-фильтром).
        var companyExists = await _db.Companies
            .AnyAsync(c => c.Id == companyId && c.Status == CompanyStatus.Active, ct);
        if (!companyExists)
            throw NotFoundException.For("Хозяйство", companyId);

        if (_current.IsSystemAdmin)
        {
            // Системный администратор видит и управляет всем (ТЗ §4).
            return new CompanyAccess
            {
                CompanyId = companyId,
                Role = AppRole.SystemAdmin,
                IsSystemAdmin = true,
                Permissions = new HashSet<string>(Permissions.All),
                HasFullScope = true,
                WarehouseIds = new HashSet<Guid>(),
                FieldIds = new HashSet<Guid>(),
            };
        }

        var membership = await _db.CompanyMemberships
            .Where(m => m.UserId == _current.UserId
                        && m.CompanyId == companyId
                        && m.Status == MembershipStatus.Active)
            .Select(m => new { m.Id, m.Role })
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            throw new ForbiddenException("Нет доступа к выбранному хозяйству.");

        var scopes = await _db.MembershipAccessScopes
            .Where(s => s.MembershipId == membership.Id)
            .Select(s => new { s.ScopeType, s.ScopeEntityId })
            .ToListAsync(ct);

        var hasFullScope = scopes.Any(s => s.ScopeType == AccessScopeType.Company);
        var warehouseIds = scopes
            .Where(s => s.ScopeType == AccessScopeType.Warehouse && s.ScopeEntityId is not null)
            .Select(s => s.ScopeEntityId!.Value).ToHashSet();
        var fieldIds = scopes
            .Where(s => s.ScopeType == AccessScopeType.Field && s.ScopeEntityId is not null)
            .Select(s => s.ScopeEntityId!.Value).ToHashSet();

        var permissions = RolePermissionMap.Map.TryGetValue(membership.Role, out var perms)
            ? new HashSet<string>(perms)
            : new HashSet<string>();

        return new CompanyAccess
        {
            CompanyId = companyId,
            Role = membership.Role,
            IsSystemAdmin = false,
            Permissions = permissions,
            HasFullScope = hasFullScope,
            WarehouseIds = warehouseIds,
            FieldIds = fieldIds,
        };
    }
}
