using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Constants;

namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Реализация текущего пользователя для MVP: всегда системный пользователь (ТЗ §6).
/// При вводе авторизации заменяется на чтение из HTTP-контекста.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid UserId => SystemIds.SystemUserId;

    /// <summary>До авторизации все записи привязываются к дефолтному хозяйству (ТЗ §25).</summary>
    public Guid CompanyId => SystemIds.DefaultCompanyId;
}
