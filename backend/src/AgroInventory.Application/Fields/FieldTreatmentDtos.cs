using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Fields;

public sealed record FieldTreatmentDto(
    Guid Id,
    DateTimeOffset TreatedAt,
    Guid FieldId,
    string FieldNumber,
    Guid ChemicalId,
    string ChemicalName,
    MeasureUnit MeasureUnit,
    Guid WarehouseId,
    string WarehouseNumber,
    Guid CropId,
    string CropName,
    decimal Quantity,
    decimal? RatePerHectare,
    Guid MovementId,
    string? Comment);

public sealed record CreateFieldTreatmentRequest(
    Guid FieldId,
    Guid ChemicalId,
    Guid WarehouseId,
    Guid CropId,
    decimal? Quantity,
    decimal? RatePerHectare,
    DateTimeOffset? TreatedAt,
    string? Comment);
