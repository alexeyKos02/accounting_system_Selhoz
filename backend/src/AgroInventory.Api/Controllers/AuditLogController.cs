using AgroInventory.Api.Security;
using AgroInventory.Application.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Журнал аудита (ТЗ §21): SystemAdmin видит общий журнал, пользователь с audit.view —
/// только записи выбранного хозяйства.
/// </summary>
[ApiController]
[Route("api/audit-log")]
[Authorize]
public sealed class AuditLogController : ControllerBase
{
    private readonly AuditQueryService _service;

    public AuditLogController(AuditQueryService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<AuditLogDto>> Get([FromQuery] AuditQuery query, CancellationToken ct) =>
        await _service.GetAsync(query, ct);
}
