using AgroInventory.Api.Security;
using AgroInventory.Application.Fields;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Сезоны поля: культура на поле в конкретном году/периоде.</summary>
[ApiController]
[Route("api/field-seasons")]
public sealed class FieldSeasonsController : ControllerBase
{
    private readonly FieldSeasonService _service;

    public FieldSeasonsController(FieldSeasonService service) => _service = service;

    [HttpGet]
    [RequireCompany(Permissions.FieldsView)]
    public async Task<IReadOnlyList<FieldSeasonDto>> GetAll([FromQuery] Guid? fieldId, CancellationToken ct) =>
        await _service.GetAllAsync(fieldId, ct);

    [HttpPost]
    [RequireCompany(Permissions.FieldsManage)]
    public async Task<ActionResult<FieldSeasonDto>> Create(CreateFieldSeasonRequest request, CancellationToken ct)
    {
        var season = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { fieldId = season.FieldId }, season);
    }

    [HttpPut("{id:guid}")]
    [RequireCompany(Permissions.FieldsManage)]
    public async Task<FieldSeasonDto> Update(Guid id, UpdateFieldSeasonRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
