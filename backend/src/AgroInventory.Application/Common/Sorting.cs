namespace AgroInventory.Application.Common;

public enum SortDirection { Asc = 0, Desc = 1 }

/// <summary>
/// Поля сортировки списка химии (ТЗ §16.3). В MVP используется только Name,
/// остальные заложены архитектурно.
/// </summary>
public enum ChemicalSortBy { Name = 0, TotalLiters = 1, LastOperation = 2, StockStatus = 3 }
