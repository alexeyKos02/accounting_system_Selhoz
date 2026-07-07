using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Запись опасных действий в audit_log (ТЗ §21). Пишет от текущего пользователя.
/// НЕ вызывает SaveChanges — запись добавляется в контекст и сохраняется вместе с операцией.
/// </summary>
public interface IAuditLogger
{
    void Log(AuditAction action, string entityType, Guid entityId, object? oldValues, object? newValues);
}
