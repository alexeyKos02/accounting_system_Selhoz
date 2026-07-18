using AgroInventory.Application.Chemicals;
using AgroInventory.Application.History;
using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Dashboard;

/// <summary>Строка блока остатков на дашборде (ТЗ §16.2 — статусы «закончилась»/«малый остаток»).</summary>
public sealed record DashboardStockDto(
    Guid ChemicalId,
    string Name,
    MeasureUnit MeasureUnit,
    decimal TotalQuantity,
    StockStatus Status);

/// <summary>
/// Сводка для дашборда (ТЗ §22): блоки «Закончилась» / «Малый остаток», список химии,
/// последние операции.
/// </summary>
public sealed record DashboardDto(
    int ActiveChemicals,
    int Warehouses,
    int EmptyCount,
    int LowCount,
    decimal LowStockThresholdLiters,
    decimal LowStockThresholdKg,
    IReadOnlyList<DashboardStockDto> Empty,
    IReadOnlyList<DashboardStockDto> Low,
    IReadOnlyList<DashboardStockDto> Chemicals,
    IReadOnlyList<HistoryItemDto> RecentOperations);

public sealed record AllCompaniesDashboardQuery(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null);

public sealed record AllCompaniesDashboardAlertDto(
    Guid CompanyId,
    string CompanyName,
    Guid ChemicalId,
    string ChemicalName,
    MeasureUnit MeasureUnit,
    decimal TotalQuantity,
    StockStatus Status);

public sealed record AllCompaniesDashboardDto(
    decimal LowStockThresholdLiters,
    decimal LowStockThresholdKg,
    IReadOnlyList<AllCompaniesDashboardAlertDto> Low,
    IReadOnlyList<AllCompaniesDashboardAlertDto> Empty,
    IReadOnlyList<HistoryItemDto> RecentOperations);
