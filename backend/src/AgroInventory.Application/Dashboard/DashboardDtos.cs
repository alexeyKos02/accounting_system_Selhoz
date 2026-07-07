using AgroInventory.Application.Chemicals;
using AgroInventory.Application.History;

namespace AgroInventory.Application.Dashboard;

/// <summary>Строка блока остатков на дашборде (ТЗ §16.2 — статусы «закончилась»/«малый остаток»).</summary>
public sealed record DashboardStockDto(
    Guid ChemicalId,
    string Name,
    decimal TotalLiters,
    StockStatus Status);

/// <summary>
/// Сводка для дашборда (ТЗ §22): счётчики, блоки «Закончилась» / «Малый остаток»,
/// последние операции.
/// </summary>
public sealed record DashboardDto(
    int ActiveChemicals,
    int Warehouses,
    int EmptyCount,
    int LowCount,
    decimal TotalLiters,
    decimal LowStockThresholdLiters,
    IReadOnlyList<DashboardStockDto> Empty,
    IReadOnlyList<DashboardStockDto> Low,
    IReadOnlyList<HistoryItemDto> RecentOperations);
