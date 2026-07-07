using AgroInventory.Application.Abstractions;

namespace AgroInventory.Infrastructure.Backups;

/// <summary>
/// Заглушка хранилища бэкапов на этапе каркаса.
/// Возвращает пустой список и не падает, чтобы health-эндпоинты работали до настройки S3.
/// Заменяется реальной S3-реализацией на этапе инфраструктуры (ТЗ §24).
/// </summary>
public sealed class NotConfiguredBackupStorage : IBackupStorage
{
    public bool IsConfigured => false;

    public Task SaveBackupAsync(string fileName, Stream content, CancellationToken ct = default)
        => throw new NotSupportedException("S3-хранилище бэкапов ещё не настроено.");

    public Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BackupInfo>>(Array.Empty<BackupInfo>());

    public Task<Stream> DownloadBackupAsync(string fileName, CancellationToken ct = default)
        => throw new NotSupportedException("S3-хранилище бэкапов ещё не настроено.");
}
