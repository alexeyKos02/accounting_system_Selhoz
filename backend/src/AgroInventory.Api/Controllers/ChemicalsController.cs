using AgroInventory.Application.Chemicals;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Химия (ТЗ §15–18, §29).</summary>
[ApiController]
[Route("api/chemicals")]
public sealed class ChemicalsController : ControllerBase
{
    private readonly ChemicalService _service;

    public ChemicalsController(ChemicalService service) => _service = service;

    /// <summary>Список химии с поиском, фильтрами и сортировкой (ТЗ §16).</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ChemicalListItemDto>> GetList(
        [FromQuery] ChemicalListQuery query, CancellationToken ct) =>
        await _service.GetListAsync(query, ct);

    /// <summary>Похожие карточки для проверки дублей (ТЗ §18.1).</summary>
    [HttpGet("duplicates")]
    public async Task<IReadOnlyList<DuplicateDto>> FindDuplicates(
        [FromQuery] string name, CancellationToken ct) =>
        await _service.FindDuplicatesAsync(name, ct);

    /// <summary>Архивные карточки (ТЗ §17.2).</summary>
    [HttpGet("archived")]
    public async Task<IReadOnlyList<ArchivedChemicalDto>> GetArchived(CancellationToken ct) =>
        await _service.GetArchivedAsync(ct);

    /// <summary>Карточка химии (ТЗ §15).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ChemicalDetailDto> GetById(Guid id, CancellationToken ct) =>
        await _service.GetByIdAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<ChemicalDetailDto>> Create(CreateChemicalRequest request, CancellationToken ct)
    {
        var chemical = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = chemical.Id }, chemical);
    }

    [HttpPut("{id:guid}")]
    public async Task<ChemicalDetailDto> Update(Guid id, UpdateChemicalRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);

    /// <summary>Архивировать (ТЗ §17.1). При остатке > 0 нужно подтверждение словом «АРХИВ».</summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, ArchiveChemicalRequest request, CancellationToken ct)
    {
        await _service.ArchiveAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        await _service.RestoreAsync(id, ct);
        return NoContent();
    }

    /// <summary>Объединить дубли (ТЗ §18.2).</summary>
    [HttpPost("merge")]
    public async Task<ChemicalDetailDto> Merge(MergeChemicalsRequest request, CancellationToken ct) =>
        await _service.MergeAsync(request, ct);
}
