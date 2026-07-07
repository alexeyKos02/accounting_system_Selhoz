using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroInventory.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Username).HasMaxLength(100).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();

        // Системный пользователь: все операции и audit log в MVP пишутся от него (ТЗ §6).
        b.HasData(new User
        {
            Id = SystemIds.SystemUserId,
            Username = "system",
            DisplayName = "System",
            IsSystem = true,
            CreatedAt = SystemIds.SeedTimestamp,
            UpdatedAt = SystemIds.SeedTimestamp,
        });
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();

        b.HasData(new Role
        {
            Id = SystemIds.AdminRoleId,
            Code = "admin",
            DisplayName = "Администратор",
        });
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();

        b.HasData(Permissions.All.Select(code => new Permission
        {
            Id = SystemIds.PermissionIds[code],
            Code = code,
        }));
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("user_roles");
        b.HasKey(x => new { x.UserId, x.RoleId });

        b.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);

        // Системный пользователь получает роль администратора (все права).
        b.HasData(new UserRole { UserId = SystemIds.SystemUserId, RoleId = SystemIds.AdminRoleId });
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => new { x.RoleId, x.PermissionId });

        b.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);

        // Админ-роль получает все права.
        b.HasData(Permissions.All.Select(code => new RolePermission
        {
            RoleId = SystemIds.AdminRoleId,
            PermissionId = SystemIds.PermissionIds[code],
        }));
    }
}
