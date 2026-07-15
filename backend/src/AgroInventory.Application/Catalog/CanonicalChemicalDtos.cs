namespace AgroInventory.Application.Catalog;

/// <summary>Канонический препарат общего справочника (ТЗ §12).</summary>
public sealed record CanonicalChemicalDto(
    Guid Id,
    string CanonicalName,
    string? Manufacturer,
    string? ActiveIngredient,
    string? Concentration,
    string? Formulation,
    string? RegistrationNumber);

public sealed record CreateCanonicalChemicalRequest(
    string CanonicalName,
    string? Manufacturer,
    string? ActiveIngredient,
    string? Concentration,
    string? Formulation,
    string? RegistrationNumber);

public sealed record UpdateCanonicalChemicalRequest(
    string CanonicalName,
    string? Manufacturer,
    string? ActiveIngredient,
    string? Concentration,
    string? Formulation,
    string? RegistrationNumber);
