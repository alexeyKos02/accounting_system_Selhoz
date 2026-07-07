using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Абстракция над контекстом БД для слоя Application. EF Core здесь используется как
/// ORM-абстракция; конкретная СУБД (Npgsql) остаётся в Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<ChemicalDetails> ChemicalDetails { get; }
    DbSet<Crop> Crops { get; }
    DbSet<ChemicalCrop> ChemicalCrops { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<ChemicalStockBalance> ChemicalStockBalances { get; }
    DbSet<PackageGroup> PackageGroups { get; }
    DbSet<OpenedPackage> OpenedPackages { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<InventoryMovementDetail> InventoryMovementDetails { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<AppSettings> AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
