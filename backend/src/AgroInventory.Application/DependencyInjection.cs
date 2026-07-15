using AgroInventory.Application.Audit;
using AgroInventory.Application.Auth;
using AgroInventory.Application.Chemicals;
using AgroInventory.Application.Crops;
using AgroInventory.Application.Dashboard;
using AgroInventory.Application.Fields;
using AgroInventory.Application.Gpt;
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

        services.AddScoped<Security.CompanyContextService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<Companies.CompanyService>();
        services.AddScoped<Companies.MembershipService>();
        services.AddScoped<Catalog.CanonicalChemicalService>();
        services.AddScoped<Catalog.AggregatedChemicalService>();
        services.AddScoped<CropService>();
        services.AddScoped<WarehouseService>();
        services.AddScoped<FieldService>();
        services.AddScoped<ChemicalService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<ReceiptQueryService>();
        services.AddScoped<HistoryQueryService>();
        services.AddScoped<AuditQueryService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<GptService>();

        return services;
    }
}
