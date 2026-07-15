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
        b.Property(x => x.Manufacturer).HasMaxLength(300);
        b.Property(x => x.ActiveIngredient).HasMaxLength(300);
        b.Property(x => x.Concentration).HasMaxLength(100);
        b.Property(x => x.Formulation).HasMaxLength(100);
        b.Property(x => x.RegistrationNumber).HasMaxLength(100);
        b.HasIndex(x => x.CanonicalName);
    }
}
