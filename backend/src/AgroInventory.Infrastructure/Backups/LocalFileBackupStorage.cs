using AgroInventory.Application.Abstractions;

namespace AgroInventory.Infrastructure.Backups;

/// <summary>
/// Хранилище бэкапов в локальной папке (для разработки/on-prem без S3). Файлы — единственный
/// источник истины по бэкапам, поэтому список читается прямо с диска.
/// </summary>
public sealed class LocalFileBackupStorage : IBackupStorage
{
    private readonly string _directory;

    public LocalFileBackupStorage(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public bool IsConfigured => true;

    public async Task SaveBackupAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var path = ResolvePath(fileName);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    public Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken ct = default)
    {
        var files = new DirectoryInfo(_directory)
            .EnumerateFiles("*.json")
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new BackupInfo(f.Name, f.Length, new DateTimeOffset(f.CreationTimeUtc, TimeSpan.Zero)))
            .ToList();
        return Task.FromResult<IReadOnlyList<BackupInfo>>(files);
    }

    public Task<Stream> DownloadBackupAsync(string fileName, CancellationToken ct = default)
    {
        var path = ResolvePath(fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Бэкап «{fileName}» не найден.", fileName);
        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    /// <summary>Защита от выхода за пределы папки — берём только имя файла.</summary>
    private string ResolvePath(string fileName) => Path.Combine(_directory, Path.GetFileName(fileName));
}
