namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Текущий пользователь. В MVP авторизации нет — всегда системный пользователь (ТЗ §6).
/// Абстракция заложена под будущую авторизацию: реализацию заменим, вызовы не изменятся.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}
