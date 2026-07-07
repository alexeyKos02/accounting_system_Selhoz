namespace AgroInventory.Domain.Entities;

/// <summary>
/// Глобальные настройки приложения одной строкой (ТЗ §23). Id фиксирован (SystemIds.AppSettingsId).
/// </summary>
public class AppSettings
{
    public Guid Id { get; set; }

    /// <summary>Общий порог малого остатка в литрах (ТЗ §16.2, §23.2). Дефолт — 10 л.</summary>
    public decimal LowStockThresholdLiters { get; set; } = 10m;

    /// <summary>Автоматически вскрывать упаковки при списании (ТЗ §11.9, §23.2). Дефолт — выкл.</summary>
    public bool AutoOpenPackages { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
