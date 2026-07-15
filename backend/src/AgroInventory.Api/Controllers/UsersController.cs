using AgroInventory.Api.Security;
using AgroInventory.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Данные текущего пользователя (ТЗ §21).</summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly AuthService _service;

    public UsersController(AuthService service) => _service = service;

    [Authorize]
    [HttpGet("me")]
    public async Task<MeResponse> Me(CancellationToken ct) =>
        await _service.GetMeAsync(User.GetUserId(), ct);
}
