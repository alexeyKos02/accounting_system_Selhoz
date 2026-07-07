using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Audit;

/// <summary>Фильтры журнала аудита (ТЗ §21.2).</summary>
public sealed record AuditQuery(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    AuditAction? Action = null,
    string? EntityType = null,
    Guid? UserId = null);

public sealed record AuditLogDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    Guid UserId,
    string UserName,
    AuditAction Action,
    string EntityType,
    Guid EntityId,
    string? OldValues,
    string? NewValues);
