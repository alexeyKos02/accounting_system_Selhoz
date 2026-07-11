using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.History;

/// <summary>Фильтры истории операций (ТЗ §19.1).</summary>
public sealed record HistoryQuery(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    Guid? ChemicalId = null,
    MovementType? MovementType = null,
    Guid? WarehouseId = null,
    Guid? CropId = null,
    Guid? FieldId = null);

/// <summary>Строка истории (ТЗ §19.2).</summary>
public sealed record HistoryItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    MovementType MovementType,
    Guid ChemicalId,
    string ChemicalName,
    decimal QuantityLiters,
    Guid WarehouseId,
    string WarehouseNumber,
    Guid? CropId,
    string? CropName,
    Guid? FieldId,
    string? FieldNumber,
    string? Comment);

public sealed record HistoryDetailSourceDto(
    MovementSourceType SourceType,
    UnitType? UnitType,
    decimal? PackageVolumeLiters,
    decimal QuantityLiters,
    int? PackagesQuantity);

/// <summary>Подробности операции (ТЗ §19.2 — детали открываются отдельно).</summary>
public sealed record HistoryDetailDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    MovementType MovementType,
    Guid ChemicalId,
    string ChemicalName,
    decimal QuantityLiters,
    UnitType? UnitType,
    decimal? PackageVolumeLiters,
    int? PackagesQuantity,
    Guid WarehouseId,
    string WarehouseNumber,
    Guid? CropId,
    string? CropName,
    Guid? FieldId,
    string? FieldNumber,
    string? Comment,
    IReadOnlyList<HistoryDetailSourceDto> Sources);

/// <summary>
/// Редактирование операции (ТЗ §20). Метаданные (дата/культура/комментарий) — всегда.
/// Количество — опционально: пересчитывает остатки. Набор полей зависит от типа операции.
/// </summary>
public sealed record EditMovementRequest(
    DateTimeOffset? OccurredAt,
    Guid? CropId,
    string? Comment,
    decimal? QuantityLiters,
    UnitType? Unit,
    decimal? PackageVolumeLiters,
    int? PackagesQuantity,
    Guid? FieldId = null);
