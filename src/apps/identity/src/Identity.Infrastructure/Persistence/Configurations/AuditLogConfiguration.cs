using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.UserId);
        builder.Property(a => a.ClientApplicationId);

        builder.Property(a => a.EventType).IsRequired().HasMaxLength(100);

        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        // JSONB → JSON binario en PostgreSQL
        // Ventajas sobre TEXT: validación de formato, compresión,
        // índices GIN sobre campos del JSON, operadores de consulta
        builder.Property(a => a.Metadata).HasColumnType("jsonb");

        builder.Property(a => a.Success).IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_logs_user_id");

        builder.HasIndex(a => a.TenantId).HasDatabaseName("ix_audit_logs_tenant_id");

        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");

        builder.HasIndex(a => a.IpAddress).HasDatabaseName("ix_audit_logs_ip_address");

        // Para CountFailedLoginsFromIpAsync
        builder
            .HasIndex(a => new
            {
                a.IpAddress,
                a.EventType,
                a.CreatedAt,
            })
            .HasDatabaseName("ix_audit_logs_ip_event_date");
    }
}
