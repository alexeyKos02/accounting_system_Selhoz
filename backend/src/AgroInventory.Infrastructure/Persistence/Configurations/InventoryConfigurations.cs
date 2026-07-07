using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("inventory_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.ItemType).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ItemType);
        b.HasIndex(x => x.Name);

        b.HasOne(x => x.MergedIntoItem)
            .WithMany()
            .HasForeignKey(x => x.MergedIntoItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ChemicalDetails)
            .WithOne(d => d.InventoryItem)
            .HasForeignKey<ChemicalDetails>(d => d.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ChemicalDetailsConfiguration : IEntityTypeConfiguration<ChemicalDetails>
{
    public void Configure(EntityTypeBuilder<ChemicalDetails> b)
    {
        b.ToTable("chemical_details");
        b.HasKey(x => x.Id);
        b.Property(x => x.Manufacturer).HasMaxLength(300);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.HasIndex(x => x.InventoryItemId).IsUnique();
    }
}

public sealed class ChemicalCropConfiguration : IEntityTypeConfiguration<ChemicalCrop>
{
    public void Configure(EntityTypeBuilder<ChemicalCrop> b)
    {
        b.ToTable("chemical_crops");
        b.HasKey(x => new { x.ChemicalId, x.CropId });

        b.HasOne(x => x.Chemical)
            .WithMany(i => i.ChemicalCrops)
            .HasForeignKey(x => x.ChemicalId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Crop)
            .WithMany(c => c.ChemicalCrops)
            .HasForeignKey(x => x.CropId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
