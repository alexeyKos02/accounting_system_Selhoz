namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Параметры аутентификации (секция конфигурации Jwt, ТЗ §1). Ключ подписи в проде задаётся
/// переменной окружения (Jwt__SigningKey); в dev — в appsettings.Development.json.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "AgroInventory";
    public string Audience { get; set; } = "AgroInventory";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 30;
}

/// <summary>
/// Первичный системный администратор (секция AdminBootstrap, ТЗ §25.2). При старте, если такого
/// пользователя ещё нет, он создаётся с временным паролем (must_change_password = true).
/// </summary>
public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";

    public bool Enabled { get; set; } = true;
    public string Email { get; set; } = "admin@agro.local";
    public string Password { get; set; } = "ChangeMe123!";
    public string FirstName { get; set; } = "Системный";
    public string LastName { get; set; } = "администратор";
}
