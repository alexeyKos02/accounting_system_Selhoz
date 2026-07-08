namespace AgroInventory.Infrastructure.Gpt;

/// <summary>Конфигурация LLM (секция "Gpt"). Без API-ключа GPT-функции отключены.</summary>
public sealed class GptOptions
{
    public const string SectionName = "Gpt";

    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 60;

    public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey);
}
