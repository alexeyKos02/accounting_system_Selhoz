using AgroInventory.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Раздел «Приходы»: read-only список приходов по доступным хозяйствам.</summary>
[ApiController]
[Route("api/receipts")]
[Authorize]
public sealed class ReceiptsController : ControllerBase
{
    private readonly ReceiptQueryService _service;

    public ReceiptsController(ReceiptQueryService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<ReceiptItemDto>> Get([FromQuery] ReceiptQuery query, CancellationToken ct) =>
        await _service.GetAsync(query, ct);
}
