using AgroInventory.Application.Gpt;

namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Низкоуровневый клиент LLM (ТЗ §26). Провайдер-агностичен: реализация (OpenAI) — в Infrastructure.
/// Возвращает «сырые» предложения по именам; сопоставление со справочниками — в GptService.
/// </summary>
public interface IGptClient
{
    /// <summary>Настроен ли клиент (задан API-ключ). Если нет — GPT-функции недоступны.</summary>
    bool IsConfigured { get; }

    Task<RawOperationSuggestion> ParseOperationFromTextAsync(string text, CancellationToken ct = default);

    Task<RawOperationSuggestion> ParseOperationFromImageAsync(
        byte[] image, string contentType, CancellationToken ct = default);

    Task<RawChemicalEnrichment> EnrichChemicalAsync(string name, CancellationToken ct = default);
}
