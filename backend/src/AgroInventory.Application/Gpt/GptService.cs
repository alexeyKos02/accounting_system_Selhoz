using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Gpt;

/// <summary>
/// GPT-помощник (ТЗ §26): разбор операции из текста/фото и обогащение карточки химии. Модель
/// только предлагает — ничего не сохраняет (ТЗ §31.8). Имена из ответа сопоставляются со
/// справочниками, чтобы подставить id в форму.
/// </summary>
public sealed class GptService
{
    private readonly IGptClient _gpt;
    private readonly IApplicationDbContext _db;

    public GptService(IGptClient gpt, IApplicationDbContext db)
    {
        _gpt = gpt;
        _db = db;
    }

    public bool IsConfigured => _gpt.IsConfigured;

    public async Task<OperationSuggestionDto> ParseTextAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ValidationException(nameof(text), "Введите текст для распознавания.");

        var raw = await _gpt.ParseOperationFromTextAsync(text.Trim(), ct);
        return await BuildOperationAsync(raw, ct);
    }

    public async Task<OperationSuggestionDto> ParsePhotoAsync(
        byte[] image, string contentType, CancellationToken ct = default)
    {
        if (image is null || image.Length == 0)
            throw new ValidationException(nameof(image), "Загрузите изображение.");

        var raw = await _gpt.ParseOperationFromImageAsync(image, contentType, ct);
        return await BuildOperationAsync(raw, ct);
    }

    public async Task<ChemicalEnrichmentDto> EnrichChemicalAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(nameof(name), "Укажите название химии.");

        var raw = await _gpt.EnrichChemicalAsync(name.Trim(), ct);

        var crops = new List<ReferenceMatchDto>();
        foreach (var cropName in (raw.Crops ?? Array.Empty<string>()).Where(c => !string.IsNullOrWhiteSpace(c)))
            crops.Add(await MatchCropAsync(cropName, ct));

        return new ChemicalEnrichmentDto(
            NullIfBlank(raw.Manufacturer), crops, NullIfBlank(raw.Comment), NullIfBlank(raw.Notes));
    }

    // ---------- Сборка предложения операции ----------

    private async Task<OperationSuggestionDto> BuildOperationAsync(RawOperationSuggestion raw, CancellationToken ct)
    {
        var chemical = string.IsNullOrWhiteSpace(raw.ChemicalName) ? null : await MatchChemicalAsync(raw.ChemicalName, ct);
        var warehouse = string.IsNullOrWhiteSpace(raw.WarehouseNumber) ? null : await MatchWarehouseAsync(raw.WarehouseNumber, ct);
        var crop = string.IsNullOrWhiteSpace(raw.CropName) ? null : await MatchCropAsync(raw.CropName, ct);

        return new OperationSuggestionDto(
            ParseOperationType(raw.OperationType),
            chemical,
            raw.Quantity is > 0 ? raw.Quantity : null,
            ParseUnit(raw.Unit),
            raw.PackageVolumeLiters is > 0 ? raw.PackageVolumeLiters : null,
            warehouse,
            crop,
            NullIfBlank(raw.Comment),
            NullIfBlank(raw.Notes));
    }

    // ---------- Сопоставление со справочниками ----------

    private async Task<ReferenceMatchDto> MatchChemicalAsync(string name, CancellationToken ct)
    {
        var candidates = await _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active)
            .Select(i => new { i.Id, i.Name })
            .ToListAsync(ct);

        var normalized = StringSimilarity.Normalize(name);
        var exact = candidates.FirstOrDefault(c => StringSimilarity.Normalize(c.Name) == normalized);
        var match = exact ?? candidates.FirstOrDefault(c => StringSimilarity.AreSimilar(c.Name, name));

        return match is null
            ? new ReferenceMatchDto(null, name.Trim(), false)
            : new ReferenceMatchDto(match.Id, match.Name, true);
    }

    private async Task<ReferenceMatchDto> MatchWarehouseAsync(string number, CancellationToken ct)
    {
        var wanted = number.Trim();
        var warehouses = await _db.Warehouses.Select(w => new { w.Id, w.Number }).ToListAsync(ct);
        var match = warehouses.FirstOrDefault(w => string.Equals(w.Number, wanted, StringComparison.OrdinalIgnoreCase));

        return match is null
            ? new ReferenceMatchDto(null, wanted, false)
            : new ReferenceMatchDto(match.Id, match.Number, true);
    }

    private async Task<ReferenceMatchDto> MatchCropAsync(string name, CancellationToken ct)
    {
        var candidates = await _db.Crops.Select(c => new { c.Id, c.Name }).ToListAsync(ct);

        var normalized = StringSimilarity.Normalize(name);
        var exact = candidates.FirstOrDefault(c => StringSimilarity.Normalize(c.Name) == normalized);
        var match = exact ?? candidates.FirstOrDefault(c => StringSimilarity.AreSimilar(c.Name, name));

        return match is null
            ? new ReferenceMatchDto(null, name.Trim(), false)
            : new ReferenceMatchDto(match.Id, match.Name, true);
    }

    // ---------- Разбор enum'ов из свободного текста ----------

    private static MovementType? ParseOperationType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ToLowerInvariant();
        if (v.Contains("income") || v.Contains("приход") || v.Contains("поступ")) return MovementType.Income;
        if (v.Contains("outcome") || v.Contains("списан") || v.Contains("расход")) return MovementType.Outcome;
        return null;
    }

    private static UnitType? ParseUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ToLowerInvariant();
        if (v.StartsWith("can") || v.Contains("банк") || v.Contains("канистр")) return UnitType.Can;
        if (v.StartsWith("piece") || v.Contains("штук") || v.StartsWith("шт")) return UnitType.Piece;
        if (v.StartsWith("lit") || v.Contains("литр") || v == "л") return UnitType.Liter;
        return null;
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
