namespace AgroInventory.Infrastructure.Backups;

/// <summary>
/// Конфигурация бэкапов (секция "Backup" в конфиге). Приоритет: S3 → локальная папка → не настроено.
/// </summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>Интервал авто-бэкапа в часах (0 или меньше — авто-бэкап выключен).</summary>
    public int AutoIntervalHours { get; set; } = 24;

    /// <summary>Локальная папка для бэкапов (для разработки/on-prem).</summary>
    public string? LocalPath { get; set; }

    public S3Options? S3 { get; set; }

    public sealed class S3Options
    {
        public string? ServiceUrl { get; set; }   // для S3-совместимых (MinIO, Backblaze, Timeweb)
        public string BucketName { get; set; } = string.Empty;
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string? Region { get; set; }
        public string Prefix { get; set; } = "backups/";
        public bool ForcePathStyle { get; set; } = true;

        public bool IsValid => !string.IsNullOrWhiteSpace(BucketName);
    }
}
