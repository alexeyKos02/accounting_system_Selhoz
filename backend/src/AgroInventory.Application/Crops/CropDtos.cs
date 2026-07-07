namespace AgroInventory.Application.Crops;

public sealed record CropDto(Guid Id, string Name);

public sealed record CreateCropRequest(string Name);

public sealed record UpdateCropRequest(string Name);
