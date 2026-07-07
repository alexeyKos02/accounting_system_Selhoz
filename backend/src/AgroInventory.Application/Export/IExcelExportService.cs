using AgroInventory.Application.History;

namespace AgroInventory.Application.Export;

/// <summary>Экспорт данных в Excel (.xlsx) — остатки и история операций (ТЗ §25).</summary>
public interface IExcelExportService
{
    Task<byte[]> ExportChemicalsAsync(CancellationToken ct = default);
    Task<byte[]> ExportHistoryAsync(HistoryQuery filter, CancellationToken ct = default);
}
