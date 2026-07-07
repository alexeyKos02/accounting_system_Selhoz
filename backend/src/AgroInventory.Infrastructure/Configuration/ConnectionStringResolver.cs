using Microsoft.Extensions.Configuration;

namespace AgroInventory.Infrastructure.Configuration;

/// <summary>
/// Разрешает строку подключения к PostgreSQL.
/// Railway отдаёт DATABASE_URL в формате postgres://user:pass@host:port/db —
/// приводим его к формату Npgsql. Иначе берём ConnectionStrings:Default.
/// </summary>
public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"]
                          ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(databaseUrl) &&
            (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://")))
        {
            return FromUrl(databaseUrl);
        }

        var fromConfig = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        throw new InvalidOperationException(
            "Не задана строка подключения к БД: укажите DATABASE_URL или ConnectionStrings:Default.");
    }

    private static string FromUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var db = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};" +
               $"Database={db};Username={Uri.UnescapeDataString(userInfo[0])};" +
               $"Password={Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : string.Empty)};" +
               "SSL Mode=Prefer;Trust Server Certificate=true";
    }
}
