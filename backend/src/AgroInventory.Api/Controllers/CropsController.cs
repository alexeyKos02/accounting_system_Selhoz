using AgroInventory.Api.Security;
using AgroInventory.Application.Crops;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Культуры (ТЗ §7.3, §29).</summary>
[ApiController]
[Route("api/crops")]
public sealed class CropsController : ControllerBase
{
    private readonly CropService _service;

    public CropsController(CropService service) => _service = service;

    [HttpGet]
    [RequireCompany(Permissions.InventoryView)]
    public async Task<IReadOnlyList<CropDto>> GetAll(CancellationToken ct) =>
        await _service.GetAllAsync(ct);

    [HttpPost]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<ActionResult<CropDto>> Create(CreateCropRequest request, CancellationToken ct)
    {
        var crop = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = crop.Id }, crop);
    }

    [HttpPut("{id:guid}")]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<CropDto> Update(Guid id, UpdateCropRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
