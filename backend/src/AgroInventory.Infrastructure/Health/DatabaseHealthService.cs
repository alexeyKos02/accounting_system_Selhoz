using AgroInventory.Application.Abstractions;
using AgroInventory.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AgroInventory.Infrastructure.Health;

/// <summary>
/// Проверяет подключение к PostgreSQL и наличие схемы (хотя бы одной пользовательской таблицы).
/// На этапе каркаса схема ещё пустая — SchemaReady будет false, пока не применена первая миграция.
/// </summary>
public sealed class DatabaseHealthService : IDatabaseHealthService
{
    private readonly IConfiguration _configuration;

    public DatabaseHealthService(IConfiguration configuration) => _configuration = configuration;

    public async Task<DatabaseHealth> CheckAsync(CancellationToken ct = default)
    {
        string connectionString;
        try
        {
            connectionString = ConnectionStringResolver.Resolve(_configuration);
        }
        catch (Exception ex)
        {
            return DatabaseHealth.Down(ex.Message);
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.tables " +
                "WHERE table_schema = 'public' AND table_type = 'BASE TABLE';",
                connection);

            var tableCount = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            return DatabaseHealth.Ok(schemaReady: tableCount > 0);
        }
        catch (Exception ex)
        {
            return DatabaseHealth.Down(ex.Message);
        }
    }
}
