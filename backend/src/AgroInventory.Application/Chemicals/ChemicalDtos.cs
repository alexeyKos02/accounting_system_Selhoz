using AgroInventory.Application.Common;
using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Chemicals;

/// <summary>Статус остатка для бейджа (ТЗ §16.2).</summary>
public enum StockStatus { InStock = 0, Low = 1, Empty = 2 }

public sealed record CropRefDto(Guid Id, string Name);

/// <summary>Строка списка химии (ТЗ §16.1). Производитель в списке не показывается.</summary>
public sealed record ChemicalListItemDto(
    Guid Id,
    string Name,
    decimal TotalLiters,
    IReadOnlyList<CropRefDto> Crops,
    StockStatus StockStatus);

public sealed record PackageGroupDto(Guid Id, UnitType UnitType, decimal PackageVolumeLiters, int Quantity);

public sealed record OpenedPackageDto(Guid Id, UnitType UnitType, decimal InitialLiters, decimal RemainingLiters);

/// <summary>Остаток по одному складу (ТЗ §15.2).</summary>
public sealed record WarehouseStockDto(
    Guid WarehouseId,
    string WarehouseNumber,
    decimal TotalLiters,
    decimal LooseLiters,
    IReadOnlyList<PackageGroupDto> PackageGroups,
    IReadOnlyList<OpenedPackageDto> OpenedPackages);

/// <summary>Карточка химии (ТЗ §15).</summary>
public sealed record ChemicalDetailDto(
    Guid Id,
    string Name,
    string? Manufacturer,
    string? Comment,
    ItemStatus Status,
    Guid? MergedIntoItemId,
    IReadOnlyList<CropRefDto> Crops,
    decimal TotalLiters,
    IReadOnlyList<WarehouseStockDto> Warehouses);

/// <summary>Карточка в архиве (ТЗ §17.2).</summary>
public sealed record ArchivedChemicalDto(
    Guid Id,
    string Name,
    string? Manufacturer,
    IReadOnlyList<CropRefDto> Crops,
    decimal TotalLiters,
    DateTimeOffset ArchivedAt);

public sealed record CreateChemicalRequest(
    string Name,
    string? Manufacturer,
    string? Comment,
    IReadOnlyList<Guid> CropIds);

public sealed record UpdateChemicalRequest(
    string Name,
    string? Manufacturer,
    string? Comment,
    IReadOnlyList<Guid> CropIds);

/// <summary>Запрос архивирования. При остатке > 0 требуется подтверждение словом «АРХИВ» (ТЗ §17.1).</summary>
public sealed record ArchiveChemicalRequest(string? Confirmation);

public sealed record DuplicateDto(Guid Id, string Name, string? Manufacturer);

/// <summary>Объединение дублей (ТЗ §18.2, §18.3). Итоговые поля выбирает пользователь.</summary>
public sealed record MergeChemicalsRequest(
    Guid TargetId,
    IReadOnlyList<Guid> SourceIds,
    string? Manufacturer,
    string? Comment,
    IReadOnlyList<Guid> CropIds);

/// <summary>Параметры списка химии (ТЗ §16.3, §16.4).</summary>
public sealed record ChemicalListQuery(
    string? Search = null,
    Guid? CropId = null,
    Guid? WarehouseId = null,
    ChemicalSortBy SortBy = ChemicalSortBy.Name,
    SortDirection SortDirection = SortDirection.Asc);
