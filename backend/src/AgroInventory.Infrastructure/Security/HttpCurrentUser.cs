using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Текущий пользователь из HTTP-контекста (ТЗ §1, §15). Если запрос не аутентифицирован —
/// откат на системного пользователя/дефолтное хозяйство, чтобы ещё не защищённые (до этапа C)
/// endpoints и фоновые задачи продолжали работать. Полная проверка членства/scope — этап C.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    /// <summary>Заголовок выбора хозяйства-контекста (переключатель хозяйств, ТЗ §15).</summary>
    public const string CompanyHeader = "X-Company-Id";

    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst(JwtClaimNames.Subject)?.Value;
            return Guid.TryParse(value, out var id) ? id : SystemIds.SystemUserId;
        }
    }

    public Guid CompanyId
    {
        get
        {
            var header = _accessor.HttpContext?.Request.Headers[CompanyHeader].ToString();
            return Guid.TryParse(header, out var id) ? id : SystemIds.DefaultCompanyId;
        }
    }
}
