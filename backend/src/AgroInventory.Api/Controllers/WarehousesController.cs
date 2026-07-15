using AgroInventory.Api.Security;
using AgroInventory.Application.Warehouses;
using AgroInventory.Domain.Constants;
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
    [RequireCompany(Permissions.WarehousesView)]
    public async Task<IReadOnlyList<WarehouseDto>> GetAll(CancellationToken ct) =>
        await _service.GetAllAsync(ct);

    [HttpPost]
    [RequireCompany(Permissions.WarehousesManage)]
    public async Task<ActionResult<WarehouseDto>> Create(CreateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = warehouse.Id }, warehouse);
    }

    [HttpPut("{id:guid}")]
    [RequireCompany(Permissions.WarehousesManage)]
    public async Task<WarehouseDto> Update(Guid id, UpdateWarehouseRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);
}
