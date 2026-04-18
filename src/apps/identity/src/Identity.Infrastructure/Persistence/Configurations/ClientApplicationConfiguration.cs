using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class ClientApplicationConfiguration : IEntityTypeConfiguration<ClientApplication>
{
    public void Configure(EntityTypeBuilder<ClientApplication> builder)
    {
        builder.ToTable("client_applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);

        // ── ClientId (Value Object) ────────────────────────
        builder
            .Property(a => a.ClientId)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(clientId => clientId.Value, value => ClientId.Create(value));

        builder.Property(a => a.ClientSecretHash).HasMaxLength(60);

        // ── RedirectUris (List<RedirectUri>) ───────────────
        //
        // Dos configuraciones necesarias:
        //
        // HasConversion → cómo serializar/deserializar la lista
        //   List<RedirectUri> → string[] (para guardar en DB)
        //   string[]          → List<RedirectUri> (al leer de DB)
        //
        // SetValueComparer → cómo detectar si la lista cambió
        //   Sin esto EF Core compara por referencia de objeto y
        //   siempre detecta cambios aunque la lista sea idéntica,
        //   generando UPDATEs innecesarios en cada SaveChanges.
        builder
            .Property(a => a.RedirectUris)
            .IsRequired()
            .HasConversion(
                uris => uris.Select(u => u.Value).ToArray(),
                values => values.Select(v => RedirectUri.Create(v, isDevelopment: true)).ToList()
            )
            .HasColumnType("text[]")
            .Metadata.SetValueComparer(
                new ValueComparer<List<RedirectUri>>(
                    // ¿Son iguales dos listas?
                    (left, right) =>
                        left != null
                        && right != null
                        && left.Count == right.Count
                        && left.Zip(right).All(p => p.First.Value == p.Second.Value),
                    // HashCode basado en los valores
                    list =>
                        list.Aggregate(
                            0,
                            (hash, uri) => HashCode.Combine(hash, uri.Value.GetHashCode())
                        ),
                    // Snapshot — copia la lista para comparar después
                    list =>
                        list.Select(u => RedirectUri.Create(u.Value, isDevelopment: true)).ToList()
                )
            );

        // List<string> — EF Core + Npgsql mapea directamente a text[]
        builder.Property(a => a.AllowedScopes).HasColumnType("text[]").IsRequired();

        builder.Property(a => a.GrantTypes).HasColumnType("text[]").IsRequired();

        builder.Property(a => a.Description).HasMaxLength(500);
        builder.Property(a => a.LogoUrl).HasMaxLength(500);

        builder.Property(a => a.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(a => a.CreatedAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(a => a.UpdatedAt).HasColumnType("timestamptz");

        // Cascade → si se elimina una app, se eliminan sus roles
        builder
            .HasMany(a => a.Roles)
            .WithOne(r => r.ClientApplication)
            .HasForeignKey(r => r.ClientApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(a => a.ClientId)
            .IsUnique()
            .HasDatabaseName("ix_client_applications_client_id");

        builder.HasIndex(a => a.TenantId).HasDatabaseName("ix_client_applications_tenant_id");
    }
}
