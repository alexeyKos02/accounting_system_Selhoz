using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class CropConfiguration : IEntityTypeConfiguration<Crop>
{
    public void Configure(EntityTypeBuilder<Crop> b)
    {
        b.ToTable("crops");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();

        // CompanyId nullable: NULL — системная культура, общая для всех хозяйств (ТЗ §8).
        // Имя уникально в пределах хозяйства (у системных — среди системных).
        b.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("warehouses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(100).IsRequired();

        // Номер склада уникален в пределах хозяйства (ТЗ §11).
        b.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique();

        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> b)
    {
        b.ToTable("fields");
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(100).IsRequired();
        b.Property(x => x.AreaHectares).HasPrecision(18, 3);

        // Номер поля уникален в пределах хозяйства (ТЗ §7).
        b.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique();

        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentCrop).WithMany().HasForeignKey(x => x.CurrentCropId).OnDelete(DeleteBehavior.Restrict);
    }
}
