using AgroInventory.Application.Security;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgroInventory.Api.Security;

/// <summary>
/// Глобальный фильтр: для действий с <see cref="RequireCompanyAttribute"/> проверяет доступ к
/// выбранному хозяйству и требуемое право до выполнения действия (ТЗ §5, §24). Ошибки доступа
/// (Forbidden/Validation/NotFound) обрабатываются ExceptionHandlingMiddleware.
/// </summary>
public sealed class CompanyAccessFilter : IAsyncActionFilter
{
    private readonly CompanyContextService _context;

    public CompanyAccessFilter(CompanyContextService context) => _context = context;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata.OfType<RequireCompanyAttribute>().LastOrDefault();
        if (attribute is not null)
        {
            if (attribute.Permission is { } permission)
                await _context.RequirePermissionAsync(permission, context.HttpContext.RequestAborted);
            else
                await _context.RequireAsync(context.HttpContext.RequestAborted);
        }

        await next();
    }
}
