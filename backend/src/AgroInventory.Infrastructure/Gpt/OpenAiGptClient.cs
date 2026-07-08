using System.Net.Http.Json;
using System.Text.Json;
using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Gpt;

namespace AgroInventory.Infrastructure.Gpt;

/// <summary>
/// LLM-клиент на OpenAI Chat Completions (ТЗ §26). Просит модель вернуть строгий JSON и
/// десериализует его в «сырые» предложения. Vision-запрос — через data-URL изображения.
/// </summary>
public sealed class OpenAiGptClient : IGptClient
{
    private const string OperationSystemPrompt =
        "Ты помощник складского учёта агрохимии. Извлеки из сообщения данные складской операции и " +
        "верни СТРОГО JSON без пояснений с полями: " +
        "operationType ('income' — приход/поступление, 'outcome' — списание/расход), " +
        "chemicalName (название препарата), quantity (число), unit ('liter'|'can'|'piece'), " +
        "packageVolumeLiters (литраж одной упаковки, если единица не литры), " +
        "warehouseNumber (номер склада строкой), cropName (культура — для списания), " +
        "comment (примечание), notes (что осталось неясным, по-русски). Неизвестное поле — null.";

    private const string EnrichSystemPrompt =
        "Ты эксперт по средствам защиты растений. По названию препарата верни СТРОГО JSON без пояснений с полями: " +
        "manufacturer (производитель или null), crops (массив культур на русском, для которых применяется), " +
        "comment (краткое назначение, 1-2 предложения), notes (оговорки/неуверенность по-русски). " +
        "Если препарат неизвестен — верни null и пустой массив, поясни в notes.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly GptOptions _options;

    public OpenAiGptClient(HttpClient http, GptOptions options)
    {
        _http = http;
        _options = options;
    }

    public bool IsConfigured => _options.IsValid;

    public Task<RawOperationSuggestion> ParseOperationFromTextAsync(string text, CancellationToken ct = default) =>
        CompleteAsync<RawOperationSuggestion>(OperationSystemPrompt, TextContent(text), ct);

    public Task<RawOperationSuggestion> ParseOperationFromImageAsync(
        byte[] image, string contentType, CancellationToken ct = default)
    {
        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(image)}";
        var content = new object[]
        {
            new { type = "text", text = "На фото накладная, этикетка или запись об операции. Извлеки данные операции." },
            new { type = "image_url", image_url = new { url = dataUrl } },
        };
        return CompleteAsync<RawOperationSuggestion>(OperationSystemPrompt, content, ct);
    }

    public Task<RawChemicalEnrichment> EnrichChemicalAsync(string name, CancellationToken ct = default) =>
        CompleteAsync<RawChemicalEnrichment>(EnrichSystemPrompt, TextContent(name), ct);

    private static object TextContent(string text) => text;

    private async Task<T> CompleteAsync<T>(string systemPrompt, object userContent, CancellationToken ct)
    {
        var body = new
        {
            model = _options.Model,
            temperature = 0.1,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent },
            },
        };

        using var response = await _http.PostAsJsonAsync("chat/completions", body, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new ConflictException($"Ошибка LLM ({(int)response.StatusCode}): {Trim(error)}");
        }

        var payload = await response.Content.ReadFromJsonAsync<OpenAiResponse>(JsonOptions, ct);
        var json = payload?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(json))
            throw new ConflictException("LLM вернул пустой ответ.");

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                   ?? throw new ConflictException("LLM вернул некорректный JSON.");
        }
        catch (JsonException)
        {
            throw new ConflictException("Не удалось разобрать ответ LLM.");
        }
    }

    private static string Trim(string s) => s.Length > 300 ? s[..300] : s;

    // ---------- Модель ответа OpenAI ----------

    private sealed class OpenAiResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }
}
