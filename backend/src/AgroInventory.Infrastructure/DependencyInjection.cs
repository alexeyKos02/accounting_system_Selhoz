using AgroInventory.Application.Abstractions;
using AgroInventory.Infrastructure.Backups;
using AgroInventory.Infrastructure.Health;
using Microsoft.Extensions.DependencyInjection;

namespace AgroInventory.Infrastructure;

/// <summary>
/// Регистрация инфраструктурных сервисов: БД (EF Core), health, S3, GPT, backup-jobs.
/// Наполняется по мере добавления функциональности.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        // TODO(этап 7): заменить на S3BackupStorage.
        services.AddSingleton<IBackupStorage, NotConfiguredBackupStorage>();

        // TODO(этап 2): регистрация AgroInventoryDbContext (Npgsql).
        return services;
    }
}
