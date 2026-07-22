using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Текущий пользователь из HTTP-контекста (ТЗ §1, §15, §24). UserId/IsSystemAdmin — из claim'ов
/// access-токена; выбранное хозяйство — из заголовка X-Company-Id. Доступ к выбранному хозяйству
/// проверяется в CompanyContextService; здесь только чтение сырых значений.
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

    public bool IsSystemAdmin =>
        _accessor.HttpContext?.User.FindFirst(JwtClaimNames.IsSystemAdmin)?.Value == "true";

    public bool CanAddToCatalog =>
        _accessor.HttpContext?.User.FindFirst(JwtClaimNames.CanAddToCatalog)?.Value == "true";

    public Guid? SelectedCompanyId
    {
        get
        {
            var header = _accessor.HttpContext?.Request.Headers[CompanyHeader].ToString();
            return Guid.TryParse(header, out var id) ? id : null;
        }
    }

    public Guid CompanyId => SelectedCompanyId ?? SystemIds.DefaultCompanyId;
}
