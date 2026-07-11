using AgroInventory.Application.Fields;
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
    public async Task<IReadOnlyList<FieldDto>> GetAll(CancellationToken ct) =>
        await _service.GetAllAsync(ct);

    [HttpPost]
    public async Task<ActionResult<FieldDto>> Create(CreateFieldRequest request, CancellationToken ct)
    {
        var field = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = field.Id }, field);
    }

    [HttpPut("{id:guid}")]
    public async Task<FieldDto> Update(Guid id, UpdateFieldRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
