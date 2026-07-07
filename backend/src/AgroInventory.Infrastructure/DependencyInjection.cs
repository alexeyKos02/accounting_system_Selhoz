using AgroInventory.Application.Abstractions;
using AgroInventory.Infrastructure.Audit;
using AgroInventory.Infrastructure.Backups;
using AgroInventory.Infrastructure.Configuration;
using AgroInventory.Infrastructure.Health;
using AgroInventory.Infrastructure.Persistence;
using AgroInventory.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroInventory.Infrastructure;

/// <summary>
/// Регистрация инфраструктурных сервисов: БД (EF Core), health, S3, GPT, backup-jobs.
/// Наполняется по мере добавления функциональности.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConnectionStringResolver.Resolve(configuration);

        services.AddDbContext<AgroInventoryDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AgroInventoryDbContext>());
        services.AddSingleton<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        // TODO(этап 7): заменить на S3BackupStorage.
        services.AddSingleton<IBackupStorage, NotConfiguredBackupStorage>();

        return services;
    }
}
