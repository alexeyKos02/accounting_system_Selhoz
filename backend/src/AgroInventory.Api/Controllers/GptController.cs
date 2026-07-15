using AgroInventory.Api.Security;
using AgroInventory.Application.Common;
using AgroInventory.Application.Gpt;
using AgroInventory.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// GPT-помощник (ТЗ §26): разбор операции из текста/фото, обогащение карточки химии.
/// Возвращает только предложения — ничего не сохраняет (ТЗ §31.8).
/// </summary>
[ApiController]
[Route("api/gpt")]
public sealed class GptController : ControllerBase
{
    private const long MaxImageBytes = 10 * 1024 * 1024;

    private readonly GptService _service;

    public GptController(GptService service) => _service = service;

    /// <summary>Настроен ли GPT — фронт прячет кнопки, если нет.</summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new { configured = _service.IsConfigured });

    [HttpPost("parse-text")]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<OperationSuggestionDto> ParseText(ParseTextRequest request, CancellationToken ct) =>
        await _service.ParseTextAsync(request.Text, ct);

    [HttpPost("parse-photo")]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<OperationSuggestionDto> ParsePhoto(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException(nameof(file), "Загрузите изображение.");
        if (file.Length > MaxImageBytes)
            throw new ValidationException(nameof(file), "Изображение больше 10 МБ.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;

        return await _service.ParsePhotoAsync(stream.ToArray(), contentType, ct);
    }

    [HttpPost("enrich-chemical")]
    [RequireCompany(Permissions.InventoryManage)]
    public async Task<ChemicalEnrichmentDto> EnrichChemical(EnrichChemicalRequest request, CancellationToken ct) =>
        await _service.EnrichChemicalAsync(request.Name, ct);
}
