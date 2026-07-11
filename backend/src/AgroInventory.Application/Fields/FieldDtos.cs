namespace AgroInventory.Application.Fields;

public sealed record FieldDto(Guid Id, string Number);

public sealed record CreateFieldRequest(string Number);

public sealed record UpdateFieldRequest(string Number);
