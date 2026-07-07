using AgroInventory.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Складские операции: приход, списание, корректировка (ТЗ §9–13, §29).</summary>
[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly InventoryService _service;

    public InventoryController(InventoryService service) => _service = service;

    [HttpPost("income")]
    public async Task<IncomeResultDto> Income(IncomeRequest request, CancellationToken ct) =>
        await _service.IncomeAsync(request, ct);

    /// <summary>План списания без сохранения — для предупреждений и «Показать план» (ТЗ §11.10).</summary>
    [HttpPost("outcome/preview")]
    public async Task<OutcomePreviewResponse> PreviewOutcome(OutcomeRequest request, CancellationToken ct) =>
        await _service.PreviewOutcomeAsync(request, ct);

    [HttpPost("outcome")]
    public async Task<OutcomeResultDto> Outcome(OutcomeRequest request, CancellationToken ct) =>
        await _service.OutcomeAsync(request, ct);

    /// <summary>Предпросмотр корректировки: было/станет/разница (ТЗ §13.6).</summary>
    [HttpPost("correction/preview")]
    public async Task<CorrectionPreviewResponse> PreviewCorrection(CorrectionRequest request, CancellationToken ct) =>
        await _service.PreviewCorrectionAsync(request, ct);

    [HttpPost("correction")]
    public async Task<CorrectionResultDto> Correction(CorrectionRequest request, CancellationToken ct) =>
        await _service.CorrectionAsync(request, ct);

    /// <summary>Ведомость инвентаризации по складу: текущие остатки для сверки с фактом (ТЗ §14).</summary>
    [HttpGet("check-sheet")]
    public async Task<InventoryCheckSheetDto> CheckSheet([FromQuery] Guid warehouseId, CancellationToken ct) =>
        await _service.GetCheckSheetAsync(warehouseId, ct);

    /// <summary>Применение фактических остатков инвентаризации (ТЗ §14).</summary>
    [HttpPost("check")]
    public async Task<InventoryCheckResultDto> Check(InventoryCheckRequest request, CancellationToken ct) =>
        await _service.ApplyCheckAsync(request, ct);
}
