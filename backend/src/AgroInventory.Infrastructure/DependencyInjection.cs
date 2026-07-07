using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Backups;
using AgroInventory.Application.Export;
using AgroInventory.Infrastructure.Audit;
using AgroInventory.Infrastructure.Backups;
using AgroInventory.Infrastructure.Configuration;
using AgroInventory.Infrastructure.Export;
using AgroInventory.Infrastructure.Health;
using AgroInventory.Infrastructure.Persistence;
using AgroInventory.Infrastructure.Security;
using Amazon.Runtime;
using Amazon.S3;
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

        AddBackups(services, configuration);
        services.AddScoped<IExcelExportService, ExcelExportService>();

        return services;
    }

    /// <summary>
    /// Хранилище бэкапов: S3 → локальная папка → «не настроено» (ТЗ §24). Авто-бэкап запускается,
    /// только если хранилище настроено и интервал положительный.
    /// </summary>
    private static void AddBackups(IServiceCollection services, IConfiguration configuration)
    {
        var options = new BackupOptions();
        configuration.GetSection(BackupOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        if (options.S3?.IsValid == true)
        {
            var s3 = options.S3;
            services.AddSingleton<IAmazonS3>(_ => CreateS3Client(s3));
            services.AddSingleton<IBackupStorage>(sp => new S3BackupStorage(sp.GetRequiredService<IAmazonS3>(), s3));
        }
        else if (!string.IsNullOrWhiteSpace(options.LocalPath))
        {
            var path = options.LocalPath!;
            services.AddSingleton<IBackupStorage>(_ => new LocalFileBackupStorage(path));
        }
        else
        {
            services.AddSingleton<IBackupStorage, NotConfiguredBackupStorage>();
        }

        services.AddScoped<IBackupService, BackupService>();

        if (options.AutoIntervalHours > 0 && (options.S3?.IsValid == true || !string.IsNullOrWhiteSpace(options.LocalPath)))
            services.AddHostedService<BackupBackgroundService>();
    }

    private static IAmazonS3 CreateS3Client(BackupOptions.S3Options s3)
    {
        var config = new AmazonS3Config { ForcePathStyle = s3.ForcePathStyle };
        if (!string.IsNullOrWhiteSpace(s3.ServiceUrl))
            config.ServiceURL = s3.ServiceUrl;
        else if (!string.IsNullOrWhiteSpace(s3.Region))
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3.Region);

        return !string.IsNullOrWhiteSpace(s3.AccessKey) && !string.IsNullOrWhiteSpace(s3.SecretKey)
            ? new AmazonS3Client(new BasicAWSCredentials(s3.AccessKey, s3.SecretKey), config)
            : new AmazonS3Client(config);
    }
}
