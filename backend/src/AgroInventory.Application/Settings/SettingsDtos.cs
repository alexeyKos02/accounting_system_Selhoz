namespace AgroInventory.Application.Settings;

/// <summary>Глобальные настройки приложения (ТЗ §23).</summary>
public sealed record AppSettingsDto(
    decimal LowStockThresholdLiters,
    decimal LowStockThresholdKg,
    DateTimeOffset UpdatedAt);

/// <summary>Изменение настроек (ТЗ §23.2). Опасное действие — пишется в audit_log.</summary>
public sealed record UpdateSettingsRequest(
    decimal LowStockThresholdLiters,
    decimal LowStockThresholdKg);
