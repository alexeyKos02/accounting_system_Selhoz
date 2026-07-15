using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> b)
    {
        b.ToTable("inventory_movements");
        b.HasKey(x => x.Id);
        b.Property(x => x.MovementType).HasConversion<int>();
        b.Property(x => x.UnitType).HasConversion<int>();
        b.Property(x => x.QuantityLiters).HasPrecision(18, 3);
        b.Property(x => x.PackageVolumeLiters).HasPrecision(18, 3);
        b.Property(x => x.Comment).HasMaxLength(2000);

        b.HasIndex(x => new { x.ChemicalId, x.WarehouseId });
        b.HasIndex(x => x.TargetWarehouseId);
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.IsDeleted);
        b.HasIndex(x => x.CompanyId);

        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Chemical).WithMany().HasForeignKey(x => x.ChemicalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TargetWarehouse).WithMany().HasForeignKey(x => x.TargetWarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Crop).WithMany().HasForeignKey(x => x.CropId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Field).WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Details)
            .WithOne(d => d.Movement)
            .HasForeignKey(d => d.MovementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InventoryMovementDetailConfiguration : IEntityTypeConfiguration<InventoryMovementDetail>
{
    public void Configure(EntityTypeBuilder<InventoryMovementDetail> b)
    {
        b.ToTable("inventory_movement_details");
        b.HasKey(x => x.Id);
        b.Property(x => x.SourceType).HasConversion<int>();
        b.Property(x => x.UnitType).HasConversion<int>();
        b.Property(x => x.PackageVolumeLiters).HasPrecision(18, 3);
        b.Property(x => x.QuantityLiters).HasPrecision(18, 3);

        b.HasIndex(x => x.MovementId);
    }
}
