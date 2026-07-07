using AgroInventory.Application.Audit;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Журнал аудита (ТЗ §21, §29).</summary>
[ApiController]
[Route("api/audit-log")]
public sealed class AuditLogController : ControllerBase
{
    private readonly AuditQueryService _service;

    public AuditLogController(AuditQueryService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<AuditLogDto>> Get([FromQuery] AuditQuery query, CancellationToken ct) =>
        await _service.GetAsync(query, ct);
}
