using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.OldValues).HasColumnType("jsonb");
        b.Property(x => x.NewValues).HasColumnType("jsonb");

        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.EntityType);
        b.HasIndex(x => x.Action);

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> b)
    {
        b.ToTable("app_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.LowStockThresholdLiters).HasPrecision(18, 3);

        // Единственная строка настроек с дефолтами (ТЗ §23.2).
        b.HasData(new AppSettings
        {
            Id = SystemIds.AppSettingsId,
            LowStockThresholdLiters = 10m,
            AutoOpenPackages = false,
            UpdatedAt = SystemIds.SeedTimestamp,
        });
    }
}
