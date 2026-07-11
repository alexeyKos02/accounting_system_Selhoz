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
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("warehouses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Number).IsUnique();
    }
}

public sealed class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> b)
    {
        b.ToTable("fields");
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Number).IsUnique();
    }
}
