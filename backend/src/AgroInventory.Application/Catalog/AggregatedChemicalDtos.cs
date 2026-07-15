namespace AgroInventory.Application.Catalog;

/// <summary>Остаток одного склада в разбивке общего режима (ТЗ §17).</summary>
public sealed record AggregatedWarehouseDto(Guid WarehouseId, string WarehouseNumber, decimal TotalLiters);

/// <summary>Позиция (карточка) конкретного хозяйства внутри группы (ТЗ §17).</summary>
public sealed record AggregatedPositionDto(
    Guid CompanyId,
    string CompanyName,
    Guid InventoryItemId,
    string LocalName,
    decimal TotalLiters,
    IReadOnlyList<AggregatedWarehouseDto> Warehouses);

/// <summary>
/// Группа одинаковой химии в общем режиме (ТЗ §17). Привязанные к одному каноническому препарату
/// позиции объединяются в одну группу; непривязанные — каждая своей группой (Linked = false).
/// Раздельный режим фронт получает, разворачивая Positions в отдельные строки.
/// </summary>
public sealed record AggregatedChemicalGroupDto(
    string Key,
    string Name,
    bool Linked,
    Guid? CanonicalChemicalId,
    decimal TotalLiters,
    int CompaniesCount,
    int WarehousesCount,
    IReadOnlyList<AggregatedPositionDto> Positions);
