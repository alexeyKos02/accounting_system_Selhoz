using AgroInventory.Api.Security;
using AgroInventory.Application.Fields;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Поля/участки (справочник для списания).</summary>
[ApiController]
[Route("api/fields")]
public sealed class FieldsController : ControllerBase
{
    private readonly FieldService _service;

    public FieldsController(FieldService service) => _service = service;

    [HttpGet]
    [RequireCompany(Permissions.FieldsView)]
    public async Task<IReadOnlyList<FieldDto>> GetAll(CancellationToken ct) =>
        await _service.GetAllAsync(ct);

    [HttpPost]
    [RequireCompany(Permissions.FieldsManage)]
    public async Task<ActionResult<FieldDto>> Create(CreateFieldRequest request, CancellationToken ct)
    {
        var field = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = field.Id }, field);
    }

    [HttpPut("{id:guid}")]
    [RequireCompany(Permissions.FieldsManage)]
    public async Task<FieldDto> Update(Guid id, UpdateFieldRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
