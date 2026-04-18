using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        // ValueGeneratedNever → EF Core no genera el ID
        // Nosotros lo generamos en el Domain con Guid.NewGuid()

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);

        builder.Property(t => t.Domain).IsRequired().HasMaxLength(253);
        // 253 → longitud máxima de un dominio según RFC 1035

        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnType("timestamptz");
        // timestamptz → timestamp with time zone en PostgreSQL
        // Siempre con timezone — nunca datetime "naive"

        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");

        builder.HasIndex(t => t.Domain).IsUnique().HasDatabaseName("ix_tenants_domain");
    }
}
