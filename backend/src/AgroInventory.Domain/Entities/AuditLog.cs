using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Журнал изменений системы (ТЗ §21). Не складская история, а история опасных действий:
/// изменения карточек, архив/восстановление, merge, редактирование/удаление операций,
/// корректировки, изменение настроек, восстановление БД. old/new — jsonb.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
