using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Backups;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Infrastructure.Backups;

/// <summary>
/// Логический бэкап БД (ТЗ §24): снимок пользовательских таблиц в JSON. Таблицы безопасности
/// (users/roles/permissions) не трогаем — они приходят из seed-миграции. Восстановление
/// заменяет пользовательские данные в одной транзакции.
/// </summary>
public sealed class BackupService : IBackupService
{
    private const string EntityType = "Database";

    private readonly AgroInventoryDbContext _db;
    private readonly IBackupStorage _storage;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    // Навигационные свойства в дамп не пишем: только скаляры + FK-идентификаторы.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoreNavigationProperties },
        },
    };

    public BackupService(
        AgroInventoryDbContext db, IBackupStorage storage, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _clock = clock;
    }

    public bool IsConfigured => _storage.IsConfigured;

    public async Task<BackupInfo> CreateAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        var snapshot = new BackupSnapshot
        {
            CreatedAt = _clock.GetUtcNow(),
            Crops = await _db.Crops.AsNoTracking().ToListAsync(ct),
            Warehouses = await _db.Warehouses.AsNoTracking().ToListAsync(ct),
            InventoryItems = await _db.InventoryItems.AsNoTracking().ToListAsync(ct),
            ChemicalDetails = await _db.ChemicalDetails.AsNoTracking().ToListAsync(ct),
            ChemicalCrops = await _db.ChemicalCrops.AsNoTracking().ToListAsync(ct),
            Balances = await _db.ChemicalStockBalances.AsNoTracking().ToListAsync(ct),
            Movements = await _db.InventoryMovements.AsNoTracking().ToListAsync(ct),
            AuditLogs = await _db.AuditLogs.AsNoTracking().ToListAsync(ct),
            AppSettings = await _db.AppSettings.AsNoTracking().ToListAsync(ct),
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        var fileName = $"agroinventory-{_clock.GetUtcNow():yyyyMMdd-HHmmss}.json";

        using var stream = new MemoryStream(bytes);
        await _storage.SaveBackupAsync(fileName, stream, ct);

        return new BackupInfo(fileName, bytes.LongLength, snapshot.CreatedAt);
    }

    public Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken ct = default) =>
        _storage.GetBackupsAsync(ct);

    public Task<Stream> DownloadAsync(string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();
        return _storage.DownloadBackupAsync(fileName, ct);
    }

    public async Task<BackupRestoreResultDto> RestoreAsync(string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();

        BackupSnapshot snapshot;
        await using (var stream = await _storage.DownloadBackupAsync(fileName, ct))
        {
            snapshot = await JsonSerializer.DeserializeAsync<BackupSnapshot>(stream, JsonOptions, ct)
                ?? throw new ConflictException("Файл бэкапа пуст или повреждён.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Чистим пользовательские таблицы в обратном по FK порядке (кроме app_settings и security).
        await _db.AuditLogs.ExecuteDeleteAsync(ct);
        await _db.InventoryMovements.ExecuteDeleteAsync(ct);
        await _db.ChemicalStockBalances.ExecuteDeleteAsync(ct);
        await _db.ChemicalCrops.ExecuteDeleteAsync(ct);
        await _db.ChemicalDetails.ExecuteDeleteAsync(ct);
        await _db.InventoryItems.ExecuteDeleteAsync(ct);
        await _db.Warehouses.ExecuteDeleteAsync(ct);
        await _db.Crops.ExecuteDeleteAsync(ct);

        // Вставляем в прямом порядке. Самоссылку merged_into_item_id проставляем вторым проходом,
        // чтобы не упереться в FK при вставке карточек-дублей.
        var mergedLinks = snapshot.InventoryItems
            .Where(i => i.MergedIntoItemId is not null)
            .ToDictionary(i => i.Id, i => i.MergedIntoItemId!.Value);
        foreach (var item in snapshot.InventoryItems) item.MergedIntoItemId = null;

        _db.Crops.AddRange(snapshot.Crops);
        _db.Warehouses.AddRange(snapshot.Warehouses);
        _db.InventoryItems.AddRange(snapshot.InventoryItems);
        _db.ChemicalDetails.AddRange(snapshot.ChemicalDetails);
        _db.ChemicalCrops.AddRange(snapshot.ChemicalCrops);
        _db.ChemicalStockBalances.AddRange(snapshot.Balances);
        _db.InventoryMovements.AddRange(snapshot.Movements);
        _db.AuditLogs.AddRange(snapshot.AuditLogs);
        await _db.SaveChangesAsync(ct);

        foreach (var (itemId, mergedInto) in mergedLinks)
        {
            var item = await _db.InventoryItems.FirstAsync(i => i.Id == itemId, ct);
            item.MergedIntoItemId = mergedInto;
        }

        // Настройки — одна строка: обновляем существующую (её не удаляли).
        if (snapshot.AppSettings.FirstOrDefault() is { } restoredSettings)
        {
            var current = await _db.AppSettings.FirstOrDefaultAsync(ct);
            if (current is null)
            {
                _db.AppSettings.Add(restoredSettings);
            }
            else
            {
                current.LowStockThresholdLiters = restoredSettings.LowStockThresholdLiters;
                current.LowStockThresholdKg = restoredSettings.LowStockThresholdKg;
                current.UpdatedAt = restoredSettings.UpdatedAt;
            }
        }

        var rows = snapshot.TotalRows;
        _audit.Log(AuditAction.Restore, EntityType, Guid.Empty, null, new { fileName, rows });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BackupRestoreResultDto(BackupSnapshot.TableCount, rows);
    }

    private void EnsureConfigured()
    {
        if (!_storage.IsConfigured)
            throw new ConflictException("Хранилище бэкапов не настроено (S3 или локальная папка).");
    }

    /// <summary>Убирает из JSON навигационные свойства (ссылки на сущности и коллекции сущностей).</summary>
    private static void IgnoreNavigationProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type.Namespace != typeof(Crop).Namespace) return;

        foreach (var property in typeInfo.Properties)
        {
            if (IsEntityType(property.PropertyType) || IsEntityCollection(property.PropertyType))
                property.ShouldSerialize = static (_, _) => false;
        }
    }

    private static bool IsEntityType(Type t) => t.Namespace == typeof(Crop).Namespace;

    private static bool IsEntityCollection(Type t)
    {
        if (t == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(t)) return false;
        var itemType = t.IsGenericType ? t.GetGenericArguments().FirstOrDefault() : t.GetElementType();
        return itemType is not null && itemType.Namespace == typeof(Crop).Namespace;
    }

    /// <summary>Снимок пользовательских таблиц. Порядок полей = порядок вставки по FK.</summary>
    private sealed class BackupSnapshot
    {
        public const int TableCount = 9;

        public int Version { get; set; } = 1;
        public DateTimeOffset CreatedAt { get; set; }

        public List<Crop> Crops { get; set; } = new();
        public List<Warehouse> Warehouses { get; set; } = new();
        public List<InventoryItem> InventoryItems { get; set; } = new();
        public List<ChemicalDetails> ChemicalDetails { get; set; } = new();
        public List<ChemicalCrop> ChemicalCrops { get; set; } = new();
        public List<ChemicalStockBalance> Balances { get; set; } = new();
        public List<InventoryMovement> Movements { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
        public List<AppSettings> AppSettings { get; set; } = new();

        public int TotalRows =>
            Crops.Count + Warehouses.Count + InventoryItems.Count + ChemicalDetails.Count +
            ChemicalCrops.Count + Balances.Count +
            Movements.Count + AuditLogs.Count + AppSettings.Count;
    }
}
