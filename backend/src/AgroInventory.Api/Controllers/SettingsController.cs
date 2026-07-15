using AgroInventory.Api.Security;
using AgroInventory.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Глобальные настройки приложения (ТЗ §23). Чтение доступно любому авторизованному
/// (фронт использует их везде), изменение — только системному администратору.
/// </summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly SettingsService _service;

    public SettingsController(SettingsService service) => _service = service;

    [HttpGet]
    public async Task<AppSettingsDto> Get(CancellationToken ct) => await _service.GetAsync(ct);

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
    public async Task<AppSettingsDto> Update(UpdateSettingsRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(request, ct);
}
