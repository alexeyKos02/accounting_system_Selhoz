namespace AgroInventory.Application.Fields;

public sealed record FieldDto(
    Guid Id,
    string Number,
    decimal? AreaHectares,
    Guid? CurrentCropId,
    string? CurrentCropName);

public sealed record CreateFieldRequest(string Number, decimal? AreaHectares, Guid? CurrentCropId);

public sealed record UpdateFieldRequest(string Number, decimal? AreaHectares, Guid? CurrentCropId);
