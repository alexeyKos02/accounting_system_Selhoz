using AgroInventory.Api.Security;
using AgroInventory.Application.History;
using AgroInventory.Application.Inventory;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>История складских операций (ТЗ §19, §20, §29). По умолчанию просмотр; правки — inventory.manage.</summary>
[ApiController]
[Route("api/history")]
[RequireCompany(Permissions.InventoryView)]
public sealed class HistoryController : ControllerBase
{
    private readonly HistoryQueryService _query;
    private readonly InventoryService _inventory;

    public HistoryController(HistoryQueryService query, InventoryService inventory)
    {
        _query = query;
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<IReadOnlyList<HistoryItemDto>> Get([FromQuery] HistoryQuery query, CancellationToken ct) =>
        await _query.GetAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<HistoryDetailDto> GetById(Guid id, CancellationToken ct) =>
        await _query.GetByIdAsync(id, ct);

    /// <summary>Редактирование операции с пересчётом остатков (ТЗ §20).</summary>
    [HttpPut("{id:guid}")]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<IActionResult> Update(Guid id, EditMovementRequest request, CancellationToken ct)
    {
        await _inventory.EditMovementAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Мягкое удаление операции (ТЗ §20).</summary>
    [HttpDelete("{id:guid}")]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _inventory.DeleteMovementAsync(id, ct);
        return NoContent();
    }
}
