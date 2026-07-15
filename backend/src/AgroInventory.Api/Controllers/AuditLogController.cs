using AgroInventory.Api.Security;
using AgroInventory.Application.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Журнал аудита (ТЗ §21). Пока audit_logs без company_id (ТЗ §23 — этап I), запись нельзя
/// фильтровать по хозяйству, поэтому чтение временно ограничено SystemAdmin, чтобы избежать
/// межхозяйственной утечки. Аудит для Owner/CompanyAdmin в рамках хозяйства — этап I.
/// </summary>
[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
public sealed class AuditLogController : ControllerBase
{
    private readonly AuditQueryService _service;

    public AuditLogController(AuditQueryService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<AuditLogDto>> Get([FromQuery] AuditQuery query, CancellationToken ct) =>
        await _service.GetAsync(query, ct);
}
