using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserApplicationRoleConfiguration : IEntityTypeConfiguration<UserApplicationRole>
{
    public void Configure(EntityTypeBuilder<UserApplicationRole> builder)
    {
        builder.ToTable("user_application_roles");

        // PK compuesta — garantiza que no se puede asignar
        // el mismo rol dos veces al mismo usuario en la misma app
        builder.HasKey(r => new
        {
            r.UserId,
            r.ClientApplicationId,
            r.RoleId,
        });

        builder.Property(r => r.AssignedAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(r => r.AssignedBy);

        // Restrict en todos — no se puede borrar User, App o Role
        // si tiene UserApplicationRoles asociados
        // Fuerza el proceso correcto: primero revocar, luego borrar
        builder
            .HasOne(r => r.User)
            .WithMany(u => u.ApplicationRoles)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(r => r.ClientApplication)
            .WithMany()
            .HasForeignKey(r => r.ClientApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(r => r.Role)
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.UserId).HasDatabaseName("ix_user_application_roles_user_id");

        builder
            .HasIndex(r => r.ClientApplicationId)
            .HasDatabaseName("ix_user_application_roles_app_id");
    }
}
