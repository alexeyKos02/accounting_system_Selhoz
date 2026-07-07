namespace AgroInventory.Application.Common;

/// <summary>Сущность не найдена → HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string entity, Guid id) =>
        new($"{entity} с id {id} не найден(а).");
}

/// <summary>Конфликт бизнес-правил → HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Ошибка валидации входных данных → HTTP 400. Ключ — поле, значения — сообщения.</summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Ошибка валидации.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } }) { }
}
