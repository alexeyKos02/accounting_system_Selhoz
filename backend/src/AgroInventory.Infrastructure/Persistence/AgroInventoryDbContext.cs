using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Infrastructure.Persistence;

/// <summary>
/// Контекст БД. Имена таблиц/колонок — snake_case (EFCore.NamingConventions),
/// чтобы совпадать со схемой из ТЗ. Реализует IApplicationDbContext для слоя Application.
/// </summary>
public class AgroInventoryDbContext : DbContext, IApplicationDbContext
{
    public AgroInventoryDbContext(DbContextOptions<AgroInventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ChemicalDetails> ChemicalDetails => Set<ChemicalDetails>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<ChemicalCrop> ChemicalCrops => Set<ChemicalCrop>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<ChemicalStockBalance> ChemicalStockBalances => Set<ChemicalStockBalance>();
    public DbSet<PackageGroup> PackageGroups => Set<PackageGroup>();
    public DbSet<OpenedPackage> OpenedPackages => Set<OpenedPackage>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<InventoryMovementDetail> InventoryMovementDetails => Set<InventoryMovementDetail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();
    public DbSet<MembershipAccessScope> MembershipAccessScopes => Set<MembershipAccessScope>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroInventoryDbContext).Assembly);
    }
}
