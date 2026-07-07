using Microsoft.Extensions.DependencyInjection;

namespace AgroInventory.Application;

/// <summary>
/// Регистрация сервисов слоя Application (use-cases, валидаторы).
/// Наполняется по мере добавления функциональности.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TODO(этап 3+): регистрация application-сервисов и FluentValidation-валидаторов.
        return services;
    }
}
