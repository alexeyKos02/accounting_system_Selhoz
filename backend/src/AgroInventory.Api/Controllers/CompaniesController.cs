using AgroInventory.Api.Security;
using AgroInventory.Application.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Хозяйства и доступы (ТЗ §21). Список — доступные пользователю; создание — только SystemAdmin;
/// изменение — по праву company.manage в этом хозяйстве. Доступ валидируется по companyId из URL.
/// </summary>
[ApiController]
[Route("api/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly CompanyService _service;

    public CompaniesController(CompanyService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<CompanyListItemDto>> GetAll(CancellationToken ct) =>
        await _service.ListAsync(ct);

    [HttpGet("{companyId:guid}")]
    public async Task<CompanyDto> Get(Guid companyId, CancellationToken ct) =>
        await _service.GetAsync(companyId, ct);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
    public async Task<ActionResult<CompanyDto>> Create(CreateCompanyRequest request, CancellationToken ct)
    {
        var company = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { companyId = company.Id }, company);
    }

    [HttpPut("{companyId:guid}")]
    public async Task<CompanyDto> Update(Guid companyId, UpdateCompanyRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(companyId, request, ct);
}
