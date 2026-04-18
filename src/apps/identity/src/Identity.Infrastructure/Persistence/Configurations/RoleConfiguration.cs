using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.ClientApplicationId).IsRequired();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.Property(r => r.Description).HasMaxLength(500);

        builder.Property(r => r.CreatedAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(r => r.UpdatedAt).HasColumnType("timestamptz");

        // Cascade → si se elimina el rol, se eliminan sus permisos
        builder
            .HasMany(r => r.Permissions)
            .WithOne(p => p.Role)
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Único (app + nombre) — no pueden existir dos roles
        // "docente" en la misma app, pero sí en apps diferentes
        builder
            .HasIndex(r => new { r.ClientApplicationId, r.Name })
            .IsUnique()
            .HasDatabaseName("ix_roles_app_name");
    }
}
