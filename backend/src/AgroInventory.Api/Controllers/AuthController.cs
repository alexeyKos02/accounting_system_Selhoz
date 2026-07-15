using AgroInventory.Api.Security;
using AgroInventory.Application.Auth;
using AgroInventory.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Аутентификация (ТЗ §1, §21).</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _service;

    public AuthController(AuthService service) => _service = service;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<TokenResponse> Login(LoginRequest request, CancellationToken ct) =>
        await _service.LoginAsync(request, ct);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken ct) =>
        await _service.RefreshAsync(request, ct);

    /// <summary>Выход: отзыв переданного refresh-токена. Не требует аутентификации.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await _service.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>Выход со всех устройств (ТЗ §1): отзыв всех refresh-токенов пользователя.</summary>
    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        await _service.LogoutAllAsync(User.GetUserId(), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _service.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}
