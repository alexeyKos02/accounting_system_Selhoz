using AgroInventory.Application.Audit;
using AgroInventory.Application.Chemicals;
using AgroInventory.Application.Crops;
using AgroInventory.Application.Dashboard;
using AgroInventory.Application.History;
using AgroInventory.Application.Inventory;
using AgroInventory.Application.Settings;
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
        services.AddScoped<HistoryQueryService>();
        services.AddScoped<AuditQueryService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<SettingsService>();

        return services;
    }
}
