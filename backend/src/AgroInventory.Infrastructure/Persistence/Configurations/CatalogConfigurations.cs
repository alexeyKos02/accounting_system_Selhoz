using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Общий канонический справочник препаратов (ТЗ §12). Глобальная таблица — без company_id и без
/// query-фильтра (общая для всех хозяйств; читается при привязке и в общем режиме §17).
/// </summary>
public sealed class CanonicalChemicalConfiguration : IEntityTypeConfiguration<CanonicalChemical>
{
    public void Configure(EntityTypeBuilder<CanonicalChemical> b)
    {
        b.ToTable("canonical_chemicals");
        b.HasKey(x => x.Id);
        b.Property(x => x.CanonicalName).HasMaxLength(300).IsRequired();
        b.Property(x => x.Type).HasConversion<int?>();
        b.Property(x => x.MeasureUnit).HasConversion<int>();
        b.Property(x => x.Manufacturer).HasMaxLength(300);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.HasIndex(x => x.CanonicalName);
    }
}

/// <summary>Связь канонический препарат ↔ культуры (ТЗ §12). Таблица `canonical_chemical_crops`.</summary>
public sealed class CanonicalChemicalCropConfiguration : IEntityTypeConfiguration<CanonicalChemicalCrop>
{
    public void Configure(EntityTypeBuilder<CanonicalChemicalCrop> b)
    {
        b.ToTable("canonical_chemical_crops");
        b.HasKey(x => new { x.CanonicalChemicalId, x.CropId });

        b.HasOne(x => x.CanonicalChemical)
            .WithMany(c => c.CanonicalChemicalCrops)
            .HasForeignKey(x => x.CanonicalChemicalId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Crop)
            .WithMany(c => c.CanonicalChemicalCrops)
            .HasForeignKey(x => x.CropId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
