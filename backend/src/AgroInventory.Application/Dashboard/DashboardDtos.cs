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

public sealed record AllCompaniesDashboardQuery(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null);

public sealed record AllCompaniesDashboardCompanyDto(
    Guid CompanyId,
    string CompanyName,
    int ActiveChemicals,
    int Warehouses,
    decimal TotalLiters,
    decimal IncomeLiters,
    decimal OutcomeLiters,
    int LowCount,
    int EmptyCount);

public sealed record AllCompaniesDashboardAlertDto(
    Guid CompanyId,
    string CompanyName,
    Guid ChemicalId,
    string ChemicalName,
    decimal TotalLiters,
    StockStatus Status);

public sealed record AllCompaniesDashboardDto(
    int CompaniesCount,
    int ActiveChemicals,
    int Warehouses,
    decimal TotalLiters,
    decimal IncomeLiters,
    decimal OutcomeLiters,
    int LowCount,
    int EmptyCount,
    decimal LowStockThresholdLiters,
    IReadOnlyList<AllCompaniesDashboardCompanyDto> Companies,
    IReadOnlyList<AllCompaniesDashboardAlertDto> Low,
    IReadOnlyList<AllCompaniesDashboardAlertDto> Empty,
    IReadOnlyList<HistoryItemDto> RecentOperations);
