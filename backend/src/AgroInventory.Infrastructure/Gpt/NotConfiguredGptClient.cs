using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Gpt;

namespace AgroInventory.Infrastructure.Gpt;

/// <summary>Заглушка LLM-клиента, когда API-ключ не задан. GPT-функции недоступны, но приложение работает.</summary>
public sealed class NotConfiguredGptClient : IGptClient
{
    public bool IsConfigured => false;

    public Task<RawOperationSuggestion> ParseOperationFromTextAsync(string text, CancellationToken ct = default)
        => throw NotConfigured();

    public Task<RawOperationSuggestion> ParseOperationFromImageAsync(
        byte[] image, string contentType, CancellationToken ct = default)
        => throw NotConfigured();

    public Task<RawChemicalEnrichment> EnrichChemicalAsync(string name, CancellationToken ct = default)
        => throw NotConfigured();

    private static ConflictException NotConfigured() =>
        new("GPT не настроен: задайте API-ключ в конфигурации (секция Gpt).");
}
