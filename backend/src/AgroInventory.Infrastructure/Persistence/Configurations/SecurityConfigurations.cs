using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.PasswordHash).HasMaxLength(512);
        b.Property(x => x.FirstName).HasMaxLength(200);
        b.Property(x => x.LastName).HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.DisplayName).HasMaxLength(400).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();

        // E-mail уникален среди пользователей с указанным e-mail (у системного он NULL).
        b.HasIndex(x => x.Email).IsUnique();

        // Системный пользователь: от него пишутся системные операции и audit log.
        // Он же — глобальный SystemAdmin. Не логинится (PasswordHash/Email пусты).
        b.HasData(new User
        {
            Id = SystemIds.SystemUserId,
            Email = null,
            FirstName = "System",
            LastName = string.Empty,
            DisplayName = "System",
            Status = UserStatus.Active,
            MustChangePassword = false,
            IsSystemAdmin = true,
            IsSystem = true,
            CreatedAt = SystemIds.SeedTimestamp,
            UpdatedAt = SystemIds.SeedTimestamp,
        });
    }
}

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("companies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.LegalName).HasMaxLength(300);
        b.Property(x => x.BinOrInn).HasMaxLength(50);
        b.Property(x => x.Country).HasMaxLength(100).IsRequired();
        b.Property(x => x.Timezone).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(1000);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();

        b.HasIndex(x => x.Status);

        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Дефолтное хозяйство для greenfield-старта (ТЗ §25). К нему привязываются
        // справочники/операции по умолчанию до полноценной авторизации.
        b.HasData(new Company
        {
            Id = SystemIds.DefaultCompanyId,
            Name = "Хозяйство по умолчанию",
            Country = "KZ",
            Timezone = "Asia/Almaty",
            Status = CompanyStatus.Active,
            CreatedByUserId = SystemIds.SystemUserId,
            CreatedAt = SystemIds.SeedTimestamp,
            UpdatedAt = SystemIds.SeedTimestamp,
        });
    }
}

public sealed class CompanyMembershipConfiguration : IEntityTypeConfiguration<CompanyMembership>
{
    public void Configure(EntityTypeBuilder<CompanyMembership> b)
    {
        b.ToTable("company_memberships");
        b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();

        // Один пользователь — не более одного членства в конкретном хозяйстве.
        b.HasIndex(x => new { x.UserId, x.CompanyId }).IsUnique();
        b.HasIndex(x => x.CompanyId);

        b.HasOne(x => x.User).WithMany(u => u.Memberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Company).WithMany(c => c.Memberships).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Членство системного пользователя в дефолтном хозяйстве (роль Owner).
        b.HasData(new CompanyMembership
        {
            Id = SystemIds.SystemMembershipId,
            UserId = SystemIds.SystemUserId,
            CompanyId = SystemIds.DefaultCompanyId,
            Role = AppRole.Owner,
            Status = MembershipStatus.Active,
            CreatedByUserId = SystemIds.SystemUserId,
            CreatedAt = SystemIds.SeedTimestamp,
            UpdatedAt = SystemIds.SeedTimestamp,
        });
    }
}

public sealed class MembershipAccessScopeConfiguration : IEntityTypeConfiguration<MembershipAccessScope>
{
    public void Configure(EntityTypeBuilder<MembershipAccessScope> b)
    {
        b.ToTable("membership_access_scopes");
        b.HasKey(x => x.Id);
        b.Property(x => x.ScopeType).HasConversion<int>();

        b.HasIndex(x => new { x.MembershipId, x.ScopeType });

        b.HasOne(x => x.Membership).WithMany(m => m.AccessScopes).HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Cascade);

        // Область доступа членства системного пользователя — всё хозяйство (scope_type = company).
        b.HasData(new MembershipAccessScope
        {
            Id = SystemIds.SystemMembershipScopeId,
            MembershipId = SystemIds.SystemMembershipId,
            ScopeType = AccessScopeType.Company,
            ScopeEntityId = null,
            CreatedAt = SystemIds.SeedTimestamp,
        });
    }
}
