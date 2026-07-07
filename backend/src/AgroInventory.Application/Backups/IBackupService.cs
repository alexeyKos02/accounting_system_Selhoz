using AgroInventory.Application.Abstractions;

namespace AgroInventory.Application.Backups;

/// <summary>
/// Создание/просмотр/восстановление резервных копий БД (ТЗ §24). Дамп — логический снимок
/// пользовательских таблиц в JSON, который кладётся в S3/локальное хранилище. Восстановление
/// заменяет пользовательские данные из снимка в одной транзакции.
/// </summary>
public interface IBackupService
{
    /// <summary>Настроено ли хранилище бэкапов.</summary>
    bool IsConfigured { get; }

    /// <summary>Снять бэкап сейчас и загрузить в хранилище.</summary>
    Task<BackupInfo> CreateAsync(CancellationToken ct = default);

    /// <summary>Список бэкапов из хранилища (источник истины — хранилище, не БД).</summary>
    Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken ct = default);

    /// <summary>Скачать содержимое бэкапа.</summary>
    Task<Stream> DownloadAsync(string fileName, CancellationToken ct = default);

    /// <summary>Восстановить БД из бэкапа. Опасное действие — пишется в audit_log.</summary>
    Task<BackupRestoreResultDto> RestoreAsync(string fileName, CancellationToken ct = default);
}

public sealed record BackupRestoreResultDto(int TablesRestored, int RowsRestored);
