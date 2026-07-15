using AgroInventory.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Химия в режиме «Все хозяйства» (ТЗ §17). Межхозяйственный агрегат — без заголовка X-Company-Id;
/// сервис сам ограничивает данные доступными пользователю хозяйствами и складами (ТЗ §21, §24).
/// Одинаковая химия объединяется через общий канонический справочник; разделение по хозяйствам —
/// разворотом позиций на фронте.
/// </summary>
[ApiController]
[Route("api/all-companies/chemicals")]
[Authorize]
public sealed class AllCompaniesChemicalsController : ControllerBase
{
    private readonly AggregatedChemicalService _service;

    public AllCompaniesChemicalsController(AggregatedChemicalService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<AggregatedChemicalGroupDto>> Get(CancellationToken ct) =>
        await _service.GetAsync(ct);
}
