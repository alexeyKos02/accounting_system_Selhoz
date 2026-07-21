using AgroInventory.Application.Chemicals;
using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Catalog;

/// <summary>
/// Канонический препарат общего справочника (ТЗ §12). Набор полей повторяет карточку химии:
/// тип, единица измерения, производитель, культуры, комментарий.
/// </summary>
public sealed record CanonicalChemicalDto(
    Guid Id,
    string CanonicalName,
    ChemicalType? Type,
    MeasureUnit MeasureUnit,
    string? Manufacturer,
    string? Comment,
    IReadOnlyList<CropRefDto> Crops);

public sealed record CreateCanonicalChemicalRequest(
    string CanonicalName,
    ChemicalType? Type,
    MeasureUnit MeasureUnit,
    string? Manufacturer,
    string? Comment,
    IReadOnlyList<Guid>? CropIds = null);

public sealed record UpdateCanonicalChemicalRequest(
    string CanonicalName,
    ChemicalType? Type,
    MeasureUnit MeasureUnit,
    string? Manufacturer,
    string? Comment,
    IReadOnlyList<Guid>? CropIds = null);
