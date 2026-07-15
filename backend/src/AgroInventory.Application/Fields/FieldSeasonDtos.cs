namespace AgroInventory.Application.Fields;

public sealed record FieldSeasonDto(
    Guid Id,
    Guid FieldId,
    string FieldNumber,
    Guid CropId,
    string CropName,
    int Year,
    string? Name,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? Comment);

public sealed record CreateFieldSeasonRequest(
    Guid FieldId,
    Guid CropId,
    int Year,
    string? Name,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? Comment);

public sealed record UpdateFieldSeasonRequest(
    Guid CropId,
    int Year,
    string? Name,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? Comment);
