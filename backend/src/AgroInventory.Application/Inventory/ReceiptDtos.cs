using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Inventory;

public sealed record ReceiptQuery(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    Guid? CompanyId = null,
    Guid? CanonicalChemicalId = null);

public sealed record ReceiptItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid CompanyId,
    string CompanyName,
    Guid ChemicalId,
    string ChemicalName,
    Guid? CanonicalChemicalId,
    string? CanonicalChemicalName,
    decimal Quantity,
    MeasureUnit MeasureUnit,
    Guid WarehouseId,
    string WarehouseNumber,
    string? Comment);
