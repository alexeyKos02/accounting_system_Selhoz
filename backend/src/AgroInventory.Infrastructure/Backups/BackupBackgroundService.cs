using AgroInventory.Application.Backups;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgroInventory.Infrastructure.Backups;

/// <summary>
/// Периодический авто-бэкап (ТЗ §24.2). Запускается, только если хранилище настроено и интервал
/// положительный. Ошибки логируются, но не роняют приложение.
/// </summary>
public sealed class BackupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public BackupBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<BackupBackgroundService> logger, BackupOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromHours(Math.Max(1, options.AutoIntervalHours));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var backups = scope.ServiceProvider.GetRequiredService<IBackupService>();
                if (!backups.IsConfigured) continue;

                var info = await backups.CreateAsync(stoppingToken);
                _logger.LogInformation("Авто-бэкап создан: {FileName} ({Size} байт)", info.FileName, info.SizeBytes);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось создать авто-бэкап");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
