using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Gpt;

// ---------- Сырой ответ модели (без сопоставления со справочниками) ----------

/// <summary>Что модель извлекла из текста/фото. Имена — как в источнике, без id (ТЗ §31.8).</summary>
public sealed record RawOperationSuggestion(
    string? OperationType,          // income | outcome
    string? ChemicalName,
    decimal? Quantity,
    string? Unit,                   // liter | can | piece
    decimal? PackageVolumeLiters,
    string? WarehouseNumber,
    string? CropName,
    string? Comment,
    string? Notes);

/// <summary>Предложение по карточке химии от модели.</summary>
public sealed record RawChemicalEnrichment(
    string? Type,
    string? Manufacturer,
    IReadOnlyList<string>? Crops,
    string? Comment,
    string? Notes);

// ---------- Итоговое предложение (сопоставлено со справочниками) ----------

public sealed record ReferenceMatchDto(Guid? Id, string Name, bool Matched);

/// <summary>
/// Готовое предложение операции для формы (ТЗ §26). GPT ничего не сохраняет — это только
/// заготовка, которую пользователь подтверждает обычной операцией.
/// </summary>
public sealed record OperationSuggestionDto(
    MovementType? OperationType,
    ReferenceMatchDto? Chemical,
    decimal? Quantity,
    UnitType? Unit,
    decimal? PackageVolumeLiters,
    ReferenceMatchDto? Warehouse,
    ReferenceMatchDto? Crop,
    string? Comment,
    string? Notes);

/// <summary>Предложение по карточке химии, культуры сопоставлены со справочником.</summary>
public sealed record ChemicalEnrichmentDto(
    ChemicalType? Type,
    string? Manufacturer,
    IReadOnlyList<ReferenceMatchDto> Crops,
    string? Comment,
    string? Notes);

// ---------- Запросы ----------

public sealed record ParseTextRequest(string Text);

public sealed record EnrichChemicalRequest(string Name);
