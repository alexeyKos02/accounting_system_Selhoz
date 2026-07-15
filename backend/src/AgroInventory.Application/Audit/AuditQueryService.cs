using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Audit;

/// <summary>Чтение журнала аудита (ТЗ §21).</summary>
public sealed class AuditQueryService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;

    public AuditQueryService(IApplicationDbContext db, ICurrentUser currentUser, CompanyContextService companyContext)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAsync(AuditQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!_currentUser.IsSystemAdmin)
        {
            var access = await _companyContext.RequirePermissionAsync(Permissions.AuditView, ct);
            q = q.Where(a => a.CompanyId == access.CompanyId);
        }

        if (query.DateFrom is { } from) q = q.Where(a => a.CreatedAt >= from);
        if (query.DateTo is { } to) q = q.Where(a => a.CreatedAt <= to);
        if (query.Action is { } action) q = q.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (query.UserId is { } user) q = q.Where(a => a.UserId == user);

        return await q
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(
                a.Id, a.CompanyId, a.Company != null ? a.Company.Name : null, a.CreatedAt, a.UserId, a.User.DisplayName,
                a.Action, a.EntityType, a.EntityId, a.OldValues, a.NewValues))
            .ToListAsync(ct);
    }
}
