namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Проверка доступности БД. Нужна для аварийного экрана восстановления (ТЗ §24.6):
/// если БД недоступна/пустая/без таблиц — фронт показывает экран восстановления из бэкапа.
/// </summary>
public interface IDatabaseHealthService
{
    Task<DatabaseHealth> CheckAsync(CancellationToken ct = default);
}

public sealed record DatabaseHealth(bool CanConnect, bool SchemaReady, string? Error)
{
    public static DatabaseHealth Ok(bool schemaReady) => new(true, schemaReady, null);
    public static DatabaseHealth Down(string error) => new(false, false, error);
}
