using System.Text.Json;
using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Infrastructure.Persistence;

namespace AgroInventory.Infrastructure.Audit;

/// <summary>
/// Пишет запись в audit_log (ТЗ §21) от текущего пользователя. SaveChanges не вызывает —
/// запись сохраняется вместе с бизнес-операцией в одной транзакции.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly AgroInventoryDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public AuditLogger(AgroInventoryDbContext db, ICurrentUser currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public void Log(AuditAction action, string entityType, Guid entityId, object? oldValues, object? newValues)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            CreatedAt = _clock.GetUtcNow(),
        });
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
