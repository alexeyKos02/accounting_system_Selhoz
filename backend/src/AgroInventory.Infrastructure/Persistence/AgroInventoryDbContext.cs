using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Infrastructure.Persistence;

/// <summary>
/// Контекст БД. Имена таблиц/колонок — snake_case (EFCore.NamingConventions),
/// чтобы совпадать со схемой из ТЗ.
/// </summary>
public class AgroInventoryDbContext : DbContext
{
    public AgroInventoryDbContext(DbContextOptions<AgroInventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ChemicalDetails> ChemicalDetails => Set<ChemicalDetails>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<ChemicalCrop> ChemicalCrops => Set<ChemicalCrop>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ChemicalStockBalance> ChemicalStockBalances => Set<ChemicalStockBalance>();
    public DbSet<PackageGroup> PackageGroups => Set<PackageGroup>();
    public DbSet<OpenedPackage> OpenedPackages => Set<OpenedPackage>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<InventoryMovementDetail> InventoryMovementDetails => Set<InventoryMovementDetail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroInventoryDbContext).Assembly);
    }
}
