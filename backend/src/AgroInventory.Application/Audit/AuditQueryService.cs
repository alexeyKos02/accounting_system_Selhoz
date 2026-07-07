using AgroInventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Audit;

/// <summary>Чтение журнала аудита (ТЗ §21).</summary>
public sealed class AuditQueryService
{
    private readonly IApplicationDbContext _db;

    public AuditQueryService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditLogDto>> GetAsync(AuditQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.DateFrom is { } from) q = q.Where(a => a.CreatedAt >= from);
        if (query.DateTo is { } to) q = q.Where(a => a.CreatedAt <= to);
        if (query.Action is { } action) q = q.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (query.UserId is { } user) q = q.Where(a => a.UserId == user);

        return await q
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(
                a.Id, a.CreatedAt, a.UserId, a.User.DisplayName,
                a.Action, a.EntityType, a.EntityId, a.OldValues, a.NewValues))
            .ToListAsync(ct);
    }
}
