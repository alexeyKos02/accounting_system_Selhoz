namespace AgroInventory.Application.Warehouses;

public sealed record WarehouseDto(Guid Id, string Number);

public sealed record CreateWarehouseRequest(string Number);

public sealed record UpdateWarehouseRequest(string Number);
