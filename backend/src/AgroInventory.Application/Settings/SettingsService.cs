using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Settings;

/// <summary>Чтение и изменение глобальных настроек (ТЗ §23). Изменение — опасное, пишется в audit_log.</summary>
public sealed class SettingsService
{
    private const string EntityType = "AppSettings";

    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public SettingsService(IApplicationDbContext db, IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async Task<AppSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await LoadAsync(ct);
        return new AppSettingsDto(settings.LowStockThresholdLiters, settings.LowStockThresholdKg, settings.UpdatedAt);
    }

    public async Task<AppSettingsDto> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        if (request.LowStockThresholdLiters < 0)
            throw new ValidationException(nameof(request.LowStockThresholdLiters),
                "Порог малого остатка не может быть отрицательным.");
        if (request.LowStockThresholdKg < 0)
            throw new ValidationException(nameof(request.LowStockThresholdKg),
                "Порог малого остатка не может быть отрицательным.");

        var settings = await LoadAsync(ct);
        var old = new { settings.LowStockThresholdLiters, settings.LowStockThresholdKg };

        settings.LowStockThresholdLiters = request.LowStockThresholdLiters;
        settings.LowStockThresholdKg = request.LowStockThresholdKg;
        settings.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Update, EntityType, settings.Id, old,
            new { settings.LowStockThresholdLiters, settings.LowStockThresholdKg });

        await _db.SaveChangesAsync(ct);
        return new AppSettingsDto(settings.LowStockThresholdLiters, settings.LowStockThresholdKg, settings.UpdatedAt);
    }

    /// <summary>Загружает единственную строку настроек; создаёт с дефолтами, если её ещё нет.</summary>
    private async Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        var settings = await _db.AppSettings.FirstOrDefaultAsync(ct);
        if (settings is not null) return settings;

        settings = new AppSettings
        {
            Id = SystemIds.AppSettingsId,
            LowStockThresholdLiters = 10m,
            LowStockThresholdKg = 10m,
            UpdatedAt = _clock.GetUtcNow(),
        };
        _db.AppSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }
}
