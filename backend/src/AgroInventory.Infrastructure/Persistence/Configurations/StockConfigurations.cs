using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class ChemicalStockBalanceConfiguration : IEntityTypeConfiguration<ChemicalStockBalance>
{
    public void Configure(EntityTypeBuilder<ChemicalStockBalance> b)
    {
        b.ToTable("chemical_stock_balances");
        b.HasKey(x => x.Id);
        b.Property(x => x.LooseLiters).HasPrecision(18, 3);
        b.Property(x => x.TotalLiters).HasPrecision(18, 3);

        // Один баланс на пару химия+склад (ТЗ §8.2). Химия и склад уже принадлежат одному хозяйству,
        // company_id дублируется на строке для быстрой фильтрации в общем режиме (ТЗ §13).
        b.HasIndex(x => new { x.ChemicalId, x.WarehouseId }).IsUnique();
        b.HasIndex(x => x.CompanyId);

        b.HasOne(x => x.Chemical).WithMany().HasForeignKey(x => x.ChemicalId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PackageGroupConfiguration : IEntityTypeConfiguration<PackageGroup>
{
    public void Configure(EntityTypeBuilder<PackageGroup> b)
    {
        b.ToTable("package_groups");
        b.HasKey(x => x.Id);
        b.Property(x => x.UnitType).HasConversion<int>();
        b.Property(x => x.PackageVolumeLiters).HasPrecision(18, 3);

        b.HasIndex(x => new { x.ChemicalId, x.WarehouseId });
        b.HasIndex(x => x.CompanyId);

        b.HasOne(x => x.Chemical).WithMany().HasForeignKey(x => x.ChemicalId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OpenedPackageConfiguration : IEntityTypeConfiguration<OpenedPackage>
{
    public void Configure(EntityTypeBuilder<OpenedPackage> b)
    {
        b.ToTable("opened_packages");
        b.HasKey(x => x.Id);
        b.Property(x => x.UnitType).HasConversion<int>();
        b.Property(x => x.InitialLiters).HasPrecision(18, 3);
        b.Property(x => x.RemainingLiters).HasPrecision(18, 3);

        b.HasIndex(x => new { x.ChemicalId, x.WarehouseId });
        b.HasIndex(x => x.CompanyId);

        b.HasOne(x => x.Chemical).WithMany().HasForeignKey(x => x.ChemicalId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
