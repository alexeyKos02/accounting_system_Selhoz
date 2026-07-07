using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgroInventory.Infrastructure.Persistence;

/// <summary>
/// Design-time фабрика для команд EF Core (migrations add/update).
/// Позволяет создавать миграции без запуска Api. Строка подключения — из DATABASE_URL
/// или дефолт на localhost (реального коннекта при создании миграции не требуется).
/// </summary>
public sealed class AgroInventoryDbContextFactory : IDesignTimeDbContextFactory<AgroInventoryDbContext>
{
    public AgroInventoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            is { Length: > 0 } url && (url.StartsWith("postgres://") || url.StartsWith("postgresql://"))
            ? Configuration.ConnectionStringResolver.FromUrlPublic(url)
            : "Host=localhost;Port=5432;Database=agroinventory;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AgroInventoryDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AgroInventoryDbContext(options);
    }
}
