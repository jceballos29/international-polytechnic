using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.TenantId).IsRequired();

        // ── Email (Value Object) ───────────────────────────
        // HasConversion define cómo convertir entre Email y string
        builder
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254)
            .HasConversion(email => email.Value, value => Email.Create(value));

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(60);
        // BCrypt siempre genera hashes de 60 caracteres exactos

        // ── PersonName (owned entity) ──────────────────────
        // OwnsOne → EF Core persiste el Value Object como columnas
        // de la misma tabla "users", no como tabla separada
        builder.OwnsOne(
            u => u.Name,
            name =>
            {
                // Le decimos a EF Core que la shadow property interna
                // "UserId" mapea a la columna "id" — evita conflicto
                // con la PK de User al aplicar la convención snake_case
                name.Property<Guid>("UserId").HasColumnName("id");

                name.Property(n => n.FirstName).HasColumnName("first_name").HasMaxLength(100);

                name.Property(n => n.MiddleName).HasColumnName("middle_name").HasMaxLength(100);

                name.Property(n => n.FirstLastName)
                    .HasColumnName("first_last_name")
                    .HasMaxLength(100);

                name.Property(n => n.SecondLastName)
                    .HasColumnName("second_last_name")
                    .HasMaxLength(100);
            }
        );

        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(u => u.FailedLoginAttempts).IsRequired().HasDefaultValue(0);

        builder.Property(u => u.LockedUntil).HasColumnType("timestamptz");

        builder.Property(u => u.CreatedAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(u => u.UpdatedAt).HasColumnType("timestamptz");

        builder
            .HasMany(u => u.ApplicationRoles)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Índices ────────────────────────────────────────
        // nameof → nombres de propiedades C#, no de columnas
        // EF Core resuelve internamente el nombre de columna
        builder
            .HasIndex(nameof(User.TenantId), nameof(User.Email))
            .IsUnique()
            .HasDatabaseName("ix_users_tenant_email");

        builder.HasIndex(u => u.TenantId).HasDatabaseName("ix_users_tenant_id");
    }
}
