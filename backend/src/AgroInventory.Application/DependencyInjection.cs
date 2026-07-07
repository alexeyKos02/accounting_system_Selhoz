using AgroInventory.Application.Chemicals;
using AgroInventory.Application.Crops;
using AgroInventory.Application.Inventory;
using AgroInventory.Application.Warehouses;
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
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<CropService>();
        services.AddScoped<WarehouseService>();
        services.AddScoped<ChemicalService>();
        services.AddScoped<InventoryService>();

        return services;
    }
}
