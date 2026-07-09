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

    /// <summary>Желаемый промежуток между авто-бэкапами.</summary>
    private readonly TimeSpan _interval;

    /// <summary>Как часто перепроверять «пора ли» — не реже раза в сутки, но не чаще интервала.
    /// Так рестарты приложения не плодят лишние копии при большом интервале (например, раз в месяц).</summary>
    private readonly TimeSpan _checkInterval;

    public BackupBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<BackupBackgroundService> logger, BackupOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromHours(Math.Max(1, options.AutoIntervalHours));
        _checkInterval = TimeSpan.FromHours(Math.Clamp(options.AutoIntervalHours, 1, 24));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_checkInterval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var backups = scope.ServiceProvider.GetRequiredService<IBackupService>();
                if (!backups.IsConfigured) continue;

                // Делаем бэкап, только если с последнего прошло не меньше интервала. Иначе перезапуски
                // (частые на Railway) не будут создавать копии сверх заданной периодичности (ТЗ §24.2).
                if (!await IsBackupDueAsync(backups, stoppingToken)) continue;

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

    /// <summary>Пора ли делать авто-бэкап: нет ни одного, либо самый свежий старше интервала.
    /// Ручные бэкапы тоже учитываются — они сдвигают расписание.</summary>
    private async Task<bool> IsBackupDueAsync(IBackupService backups, CancellationToken ct)
    {
        var existing = await backups.ListAsync(ct);
        if (existing.Count == 0) return true;

        var newest = existing.Max(b => b.CreatedAt);
        return DateTimeOffset.UtcNow - newest >= _interval;
    }
}
