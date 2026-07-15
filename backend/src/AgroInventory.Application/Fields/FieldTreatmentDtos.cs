namespace AgroInventory.Application.Fields;

public sealed record FieldTreatmentDto(
    Guid Id,
    DateTimeOffset TreatedAt,
    Guid FieldId,
    string FieldNumber,
    Guid ChemicalId,
    string ChemicalName,
    Guid WarehouseId,
    string WarehouseNumber,
    Guid CropId,
    string CropName,
    decimal QuantityLiters,
    decimal? RateLitersPerHectare,
    Guid MovementId,
    string? Comment);

public sealed record CreateFieldTreatmentRequest(
    Guid FieldId,
    Guid ChemicalId,
    Guid WarehouseId,
    Guid CropId,
    decimal? QuantityLiters,
    decimal? RateLitersPerHectare,
    bool AllowOpenNewPackage,
    DateTimeOffset? TreatedAt,
    string? Comment);
