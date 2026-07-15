using AgroInventory.Api.Security;
using AgroInventory.Application.Inventory;
using AgroInventory.Domain.Constants;
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
    [RequireCompany(Permissions.ReceiptsCreate)]
    public async Task<IncomeResultDto> Income(IncomeRequest request, CancellationToken ct) =>
        await _service.IncomeAsync(request, ct);

    [HttpGet("income/bulk/options")]
    public async Task<BulkIncomeOptionsDto> BulkIncomeOptions(
        [FromQuery] Guid canonicalChemicalId, CancellationToken ct) =>
        await _service.GetBulkIncomeOptionsAsync(canonicalChemicalId, ct);

    [HttpPost("income/bulk")]
    public async Task<BulkIncomeResultDto> BulkIncome(BulkIncomeRequest request, CancellationToken ct) =>
        await _service.BulkIncomeAsync(request, ct);

    /// <summary>План списания без сохранения — для предупреждений и «Показать план» (ТЗ §11.10).</summary>
    [HttpPost("outcome/preview")]
    [RequireCompany(Permissions.WriteoffsView)]
    public async Task<OutcomePreviewResponse> PreviewOutcome(OutcomeRequest request, CancellationToken ct) =>
        await _service.PreviewOutcomeAsync(request, ct);

    [HttpPost("outcome")]
    [RequireCompany(Permissions.WriteoffsCreate)]
    public async Task<OutcomeResultDto> Outcome(OutcomeRequest request, CancellationToken ct) =>
        await _service.OutcomeAsync(request, ct);

    [HttpGet("transfers")]
    [RequireCompany(Permissions.TransfersView)]
    public async Task<IReadOnlyList<TransferItemDto>> Transfers(CancellationToken ct) =>
        await _service.GetTransfersAsync(ct);

    [HttpPost("transfers")]
    [RequireCompany(Permissions.TransfersCreate)]
    public async Task<TransferResultDto> Transfer(TransferRequest request, CancellationToken ct) =>
        await _service.TransferAsync(request, ct);

    /// <summary>Предпросмотр корректировки: было/станет/разница (ТЗ §13.6).</summary>
    [HttpPost("correction/preview")]
    [RequireCompany(Permissions.InventoryView)]
    public async Task<CorrectionPreviewResponse> PreviewCorrection(CorrectionRequest request, CancellationToken ct) =>
        await _service.PreviewCorrectionAsync(request, ct);

    [HttpPost("correction")]
    [RequireCompany(Permissions.AdjustmentsCreate)]
    public async Task<CorrectionResultDto> Correction(CorrectionRequest request, CancellationToken ct) =>
        await _service.CorrectionAsync(request, ct);

    /// <summary>Ведомость инвентаризации по складу: текущие остатки для сверки с фактом (ТЗ §14).</summary>
    [HttpGet("check-sheet")]
    [RequireCompany(Permissions.InventoryView)]
    public async Task<InventoryCheckSheetDto> CheckSheet([FromQuery] Guid warehouseId, CancellationToken ct) =>
        await _service.GetCheckSheetAsync(warehouseId, ct);

    /// <summary>Применение фактических остатков инвентаризации (ТЗ §14).</summary>
    [HttpPost("check")]
    [RequireCompany(Permissions.AdjustmentsCreate)]
    public async Task<InventoryCheckResultDto> Check(InventoryCheckRequest request, CancellationToken ct) =>
        await _service.ApplyCheckAsync(request, ct);
}
