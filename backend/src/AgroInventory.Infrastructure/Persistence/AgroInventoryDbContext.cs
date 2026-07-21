using AgroInventory.Application.Abstractions;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Infrastructure.Persistence;

/// <summary>
/// Контекст БД. Имена таблиц/колонок — snake_case (EFCore.NamingConventions),
/// чтобы совпадать со схемой из ТЗ. Реализует IApplicationDbContext для слоя Application.
///
/// Изоляция данных по хозяйствам (ТЗ §24): глобальные query-фильтры на company-owned сущностях
/// автоматически ограничивают чтения текущим хозяйством (_tenantId). Доступ к самому хозяйству
/// валидируется в CompanyContextService до выполнения запросов.
/// </summary>
public class AgroInventoryDbContext : DbContext, IApplicationDbContext
{
    private readonly Guid _tenantId;

    public AgroInventoryDbContext(DbContextOptions<AgroInventoryDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _tenantId = currentUser?.CompanyId ?? SystemIds.DefaultCompanyId;
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<CanonicalChemical> CanonicalChemicals => Set<CanonicalChemical>();
    public DbSet<ChemicalDetails> ChemicalDetails => Set<ChemicalDetails>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<ChemicalCrop> ChemicalCrops => Set<ChemicalCrop>();
    public DbSet<CanonicalChemicalCrop> CanonicalChemicalCrops => Set<CanonicalChemicalCrop>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<FieldSeason> FieldSeasons => Set<FieldSeason>();
    public DbSet<FieldTreatment> FieldTreatments => Set<FieldTreatment>();
    public DbSet<ChemicalStockBalance> ChemicalStockBalances => Set<ChemicalStockBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();
    public DbSet<MembershipAccessScope> MembershipAccessScopes => Set<MembershipAccessScope>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroInventoryDbContext).Assembly);

        // Глобальные фильтры изоляции по хозяйству (ТЗ §24). Ссылка на _tenantId (поле контекста)
        // переоценивается EF для каждого запроса, поэтому фильтр берёт текущее хозяйство запроса.
        modelBuilder.Entity<InventoryItem>().HasQueryFilter(x => x.CompanyId == _tenantId);
        modelBuilder.Entity<Warehouse>().HasQueryFilter(x => x.CompanyId == _tenantId);
        modelBuilder.Entity<Field>().HasQueryFilter(x => x.CompanyId == _tenantId);
        modelBuilder.Entity<FieldSeason>().HasQueryFilter(x => x.CompanyId == _tenantId);
        modelBuilder.Entity<FieldTreatment>().HasQueryFilter(x => x.CompanyId == _tenantId);
        modelBuilder.Entity<ChemicalStockBalance>().HasQueryFilter(x => x.CompanyId == _tenantId);
        modelBuilder.Entity<InventoryMovement>().HasQueryFilter(x => x.CompanyId == _tenantId);
        // Культуры: своё хозяйство + системные (общие) культуры (ТЗ §8).
        modelBuilder.Entity<Crop>().HasQueryFilter(x => x.CompanyId == _tenantId || x.IsSystem);
    }
}
