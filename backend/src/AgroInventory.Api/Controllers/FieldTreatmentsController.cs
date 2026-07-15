using AgroInventory.Api.Security;
using AgroInventory.Application.Fields;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Обработки полей с автоматическим списанием химии со склада.</summary>
[ApiController]
[Route("api/field-treatments")]
public sealed class FieldTreatmentsController : ControllerBase
{
    private readonly FieldTreatmentService _service;

    public FieldTreatmentsController(FieldTreatmentService service) => _service = service;

    [HttpGet]
    [RequireCompany(Permissions.TreatmentsView)]
    public async Task<IReadOnlyList<FieldTreatmentDto>> GetAll([FromQuery] Guid? fieldId, CancellationToken ct) =>
        await _service.GetAllAsync(fieldId, ct);

    [HttpPost]
    [RequireCompany(Permissions.TreatmentsManage)]
    public async Task<ActionResult<FieldTreatmentDto>> Create(CreateFieldTreatmentRequest request, CancellationToken ct)
    {
        var treatment = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { fieldId = treatment.FieldId }, treatment);
    }
}
