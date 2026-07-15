using AgroInventory.Api.Security;
using AgroInventory.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Общий канонический справочник препаратов (ТЗ §12). Чтение — любому авторизованному (нужно для
/// привязки карточек и общего режима §17), создание/изменение — только SystemAdmin.
/// </summary>
[ApiController]
[Route("api/canonical-chemicals")]
[Authorize]
public sealed class CanonicalChemicalsController : ControllerBase
{
    private readonly CanonicalChemicalService _service;

    public CanonicalChemicalsController(CanonicalChemicalService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<CanonicalChemicalDto>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        await _service.ListAsync(search, ct);

    [HttpGet("{id:guid}")]
    public async Task<CanonicalChemicalDto> Get(Guid id, CancellationToken ct) =>
        await _service.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
    public async Task<ActionResult<CanonicalChemicalDto>> Create(CreateCanonicalChemicalRequest request, CancellationToken ct)
    {
        var item = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
    public async Task<CanonicalChemicalDto> Update(Guid id, UpdateCanonicalChemicalRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
