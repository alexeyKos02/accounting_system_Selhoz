using AgroInventory.Application.Abstractions;
using Amazon.S3;
using Amazon.S3.Model;

namespace AgroInventory.Infrastructure.Backups;

/// <summary>
/// Хранилище бэкапов в S3-совместимом объектном хранилище (ТЗ §24.4). Список бэкапов берётся
/// из хранилища, поэтому доступен даже если основная БД недоступна.
/// </summary>
public sealed class S3BackupStorage : IBackupStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _prefix;

    public S3BackupStorage(IAmazonS3 s3, BackupOptions.S3Options options)
    {
        _s3 = s3;
        _bucket = options.BucketName;
        _prefix = options.Prefix ?? string.Empty;
    }

    public bool IsConfigured => true;

    public async Task SaveBackupAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = _prefix + fileName,
            InputStream = content,
            ContentType = "application/json",
            AutoCloseStream = false,
        }, ct);
    }

    public async Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken ct = default)
    {
        var result = new List<BackupInfo>();
        string? token = null;
        do
        {
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = _prefix,
                ContinuationToken = token,
            }, ct);

            result.AddRange(response.S3Objects
                .Where(o => o.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .Select(o => new BackupInfo(
                    o.Key[_prefix.Length..], o.Size, new DateTimeOffset(o.LastModified.ToUniversalTime(), TimeSpan.Zero))));

            token = response.IsTruncated ? response.NextContinuationToken : null;
        } while (token is not null);

        return result.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task<Stream> DownloadBackupAsync(string fileName, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(_bucket, _prefix + Path.GetFileName(fileName), ct);
        // Копируем в память, чтобы освободить сетевой поток S3.
        var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return buffer;
    }
}
