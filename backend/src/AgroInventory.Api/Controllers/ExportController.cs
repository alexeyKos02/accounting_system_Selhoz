using AgroInventory.Api.Security;
using AgroInventory.Application.Export;
using AgroInventory.Application.History;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Экспорт в Excel (.xlsx): остатки и история операций (ТЗ §25).</summary>
[ApiController]
[Route("api/export")]
[RequireCompany(Permissions.InventoryView)]
public sealed class ExportController : ControllerBase
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IExcelExportService _export;

    public ExportController(IExcelExportService export) => _export = export;

    [HttpGet("chemicals")]
    public async Task<IActionResult> Chemicals(CancellationToken ct)
    {
        var bytes = await _export.ExportChemicalsAsync(ct);
        return File(bytes, XlsxMime, "ostatki-himii.xlsx");
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] HistoryQuery filter, CancellationToken ct)
    {
        var bytes = await _export.ExportHistoryAsync(filter, ct);
        return File(bytes, XlsxMime, "istoriya-operaciy.xlsx");
    }
}
