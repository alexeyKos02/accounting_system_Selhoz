namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Абстракция над S3-совместимым хранилищем бэкапов (ТЗ §24.4).
/// Источник истины по бэкапам — хранилище, а не основная БД:
/// список бэкапов должен возвращаться даже если БД упала.
/// Полная реализация — на этапе инфраструктуры (ТЗ §24).
/// </summary>
public interface IBackupStorage
{
    Task SaveBackupAsync(string fileName, Stream content, CancellationToken ct = default);
    Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken ct = default);
    Task<Stream> DownloadBackupAsync(string fileName, CancellationToken ct = default);
}

public sealed record BackupInfo(string FileName, long SizeBytes, DateTimeOffset CreatedAt);
