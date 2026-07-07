using AgroInventory.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Сводка для главного экрана (ТЗ §22).</summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service) => _service = service;

    [HttpGet]
    public async Task<DashboardDto> Get(CancellationToken ct) => await _service.GetAsync(ct);
}
