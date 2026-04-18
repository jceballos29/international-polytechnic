using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.RoleId).IsRequired();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);

        builder.Property(p => p.Description).HasMaxLength(500);

        builder.Property(p => p.CreatedAt).IsRequired().HasColumnType("timestamptz");

        // Único (role + nombre) — no pueden existir dos permisos
        // "grades:write" en el mismo rol
        builder
            .HasIndex(p => new { p.RoleId, p.Name })
            .IsUnique()
            .HasDatabaseName("ix_permissions_role_name");
    }
}
