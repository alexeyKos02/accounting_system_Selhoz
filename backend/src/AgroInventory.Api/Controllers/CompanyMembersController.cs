using AgroInventory.Application.Companies;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Членства пользователей в хозяйстве и области доступа (ТЗ §21). Доступ проверяется по праву
/// users.view / users.manage в хозяйстве (companyId из URL) — заголовок X-Company-Id не требуется.
/// </summary>
[ApiController]
[Route("api/companies/{companyId:guid}/members")]
public sealed class CompanyMembersController : ControllerBase
{
    private readonly MembershipService _service;

    public CompanyMembersController(MembershipService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<MemberDto>> GetAll(Guid companyId, CancellationToken ct) =>
        await _service.ListAsync(companyId, ct);

    [HttpPost]
    public async Task<MemberDto> Add(Guid companyId, AddMemberRequest request, CancellationToken ct) =>
        await _service.AddAsync(companyId, request, ct);

    [HttpPut("{membershipId:guid}")]
    public async Task<MemberDto> Update(
        Guid companyId, Guid membershipId, UpdateMemberRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(companyId, membershipId, request, ct);

    [HttpDelete("{membershipId:guid}")]
    public async Task<IActionResult> Remove(Guid companyId, Guid membershipId, CancellationToken ct)
    {
        await _service.RemoveAsync(companyId, membershipId, ct);
        return NoContent();
    }

    [HttpGet("{membershipId:guid}/scopes")]
    public async Task<MemberScopesDto> GetScopes(Guid companyId, Guid membershipId, CancellationToken ct) =>
        await _service.GetScopesAsync(companyId, membershipId, ct);

    [HttpPut("{membershipId:guid}/scopes")]
    public async Task<MemberScopesDto> UpdateScopes(
        Guid companyId, Guid membershipId, UpdateScopesRequest request, CancellationToken ct) =>
        await _service.UpdateScopesAsync(companyId, membershipId, request, ct);
}
