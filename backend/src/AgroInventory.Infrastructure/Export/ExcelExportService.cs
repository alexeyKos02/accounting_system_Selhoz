using AgroInventory.Application.Chemicals;
using AgroInventory.Application.Export;
using AgroInventory.Application.History;
using AgroInventory.Domain.Enums;
using ClosedXML.Excel;

namespace AgroInventory.Infrastructure.Export;

/// <summary>Формирует .xlsx через ClosedXML из данных чтения (ТЗ §25).</summary>
public sealed class ExcelExportService : IExcelExportService
{
    private readonly ChemicalService _chemicals;
    private readonly HistoryQueryService _history;

    public ExcelExportService(ChemicalService chemicals, HistoryQueryService history)
    {
        _chemicals = chemicals;
        _history = history;
    }

    public async Task<byte[]> ExportChemicalsAsync(CancellationToken ct = default)
    {
        var items = await _chemicals.GetListAsync(new ChemicalListQuery(), ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Остатки");
        WriteHeader(sheet, "Название", "Всего, л", "Статус", "Культуры");

        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Name;
            sheet.Cell(row, 2).Value = item.TotalLiters;
            sheet.Cell(row, 3).Value = StatusText(item.StockStatus);
            sheet.Cell(row, 4).Value = string.Join(", ", item.Crops.Select(c => c.Name));
            row++;
        }

        return Finish(workbook, sheet, lastColumn: 4);
    }

    public async Task<byte[]> ExportHistoryAsync(HistoryQuery filter, CancellationToken ct = default)
    {
        var items = await _history.GetAsync(filter, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("История");
        WriteHeader(sheet, "Дата", "Тип", "Химия", "Кол-во, л", "Склад", "Культура", "Комментарий");

        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.OccurredAt.LocalDateTime;
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            sheet.Cell(row, 2).Value = MovementText(item.MovementType);
            sheet.Cell(row, 3).Value = item.ChemicalName;
            sheet.Cell(row, 4).Value = item.QuantityLiters;
            sheet.Cell(row, 5).Value = $"Склад {item.WarehouseNumber}";
            sheet.Cell(row, 6).Value = item.CropName ?? string.Empty;
            sheet.Cell(row, 7).Value = item.Comment ?? string.Empty;
            row++;
        }

        return Finish(workbook, sheet, lastColumn: 7);
    }

    private static void WriteHeader(IXLWorksheet sheet, params string[] titles)
    {
        for (var i = 0; i < titles.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = titles[i];
            cell.Style.Font.Bold = true;
        }
    }

    private static byte[] Finish(XLWorkbook workbook, IXLWorksheet sheet, int lastColumn)
    {
        sheet.Range(1, 1, 1, lastColumn).SetAutoFilter();
        sheet.Columns(1, lastColumn).AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string StatusText(StockStatus status) => status switch
    {
        StockStatus.Empty => "Закончилась",
        StockStatus.Low => "Малый остаток",
        _ => "В наличии",
    };

    private static string MovementText(MovementType type) => type switch
    {
        MovementType.Income => "Приход",
        MovementType.Outcome => "Списание",
        MovementType.Correction => "Корректировка",
        _ => type.ToString(),
    };
}
