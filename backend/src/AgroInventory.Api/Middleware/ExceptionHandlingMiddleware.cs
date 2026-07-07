using AgroInventory.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Middleware;

/// <summary>
/// Преобразует доменные исключения Application в корректные HTTP-ответы (ProblemDetails).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var problem = new ValidationProblemDetails(
                ex.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации.",
            };
            await Write(context, problem, StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            await Write(context, Problem(ex.Message, StatusCodes.Status404NotFound, "Не найдено"),
                StatusCodes.Status404NotFound);
        }
        catch (ConflictException ex)
        {
            await Write(context, Problem(ex.Message, StatusCodes.Status409Conflict, "Конфликт"),
                StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка при обработке запроса");
            await Write(context, Problem("Внутренняя ошибка сервера.", StatusCodes.Status500InternalServerError,
                "Ошибка сервера"), StatusCodes.Status500InternalServerError);
        }
    }

    private static ProblemDetails Problem(string detail, int status, string title) =>
        new() { Detail = detail, Status = status, Title = title };

    private static async Task Write(HttpContext context, ProblemDetails problem, int status)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType());
    }
}
