namespace AgroInventory.Domain.Entities;

/// <summary>
/// Глобальные настройки приложения одной строкой (ТЗ §23). Id фиксирован (SystemIds.AppSettingsId).
/// </summary>
public class AppSettings
{
    public Guid Id { get; set; }

    /// <summary>Порог малого остатка для химии в литрах. Дефолт — 10 л.</summary>
    public decimal LowStockThresholdLiters { get; set; } = 10m;

    /// <summary>Порог малого остатка для химии в килограммах. Дефолт — 10 кг.</summary>
    public decimal LowStockThresholdKg { get; set; } = 10m;

    public DateTimeOffset UpdatedAt { get; set; }
}
