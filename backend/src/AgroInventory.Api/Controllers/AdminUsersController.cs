using AgroInventory.Api.Security;
using AgroInventory.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Управление аккаунтами системным администратором (ТЗ §21). Доступ — только SystemAdmin.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly AdminUserService _service;

    public AdminUsersController(AdminUserService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<AdminUserDto>> GetAll(CancellationToken ct) =>
        await _service.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, user);
    }

    [HttpPut("{userId:guid}")]
    public async Task<AdminUserDto> Update(Guid userId, UpdateUserRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(userId, request, ct);

    [HttpPost("{userId:guid}/block")]
    public async Task<AdminUserDto> Block(Guid userId, BlockUserRequest? request, CancellationToken ct) =>
        await _service.SetBlockedAsync(userId, request?.Blocked ?? true, ct);

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid userId, ResetPasswordRequest request, CancellationToken ct)
    {
        await _service.ResetPasswordAsync(userId, request, ct);
        return NoContent();
    }
}

/// <summary>Тело запроса блокировки/разблокировки (по умолчанию — заблокировать).</summary>
public sealed record BlockUserRequest(bool Blocked = true);
