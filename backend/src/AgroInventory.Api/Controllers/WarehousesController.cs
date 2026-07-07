using AgroInventory.Application.Warehouses;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Склады (ТЗ §7.4, §29).</summary>
[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly WarehouseService _service;

    public WarehousesController(WarehouseService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<WarehouseDto>> GetAll(CancellationToken ct) =>
        await _service.GetAllAsync(ct);

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> Create(CreateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = warehouse.Id }, warehouse);
    }

    [HttpPut("{id:guid}")]
    public async Task<WarehouseDto> Update(Guid id, UpdateWarehouseRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
