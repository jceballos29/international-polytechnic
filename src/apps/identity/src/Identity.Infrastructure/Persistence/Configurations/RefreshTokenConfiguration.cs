using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.ClientApplicationId).IsRequired();

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        // SHA256 siempre produce 64 caracteres hex

        builder.Property(t => t.Scopes).HasColumnType("text[]").IsRequired();

        builder.Property(t => t.ExpiresAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(t => t.RevokedAt).HasColumnType("timestamptz");

        builder.Property(t => t.ReplacedById);

        builder.Property(t => t.IssuedFromIp).HasMaxLength(45);
        // 45 → longitud máxima de IPv6

        builder.Property(t => t.SessionId).IsRequired().HasMaxLength(64);

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");

        builder
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(t => t.ClientApplication)
            .WithMany()
            .HasForeignKey(t => t.ClientApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // El índice más importante — se usa en cada refresh request
        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_hash");

        builder
            .HasIndex(t => new { t.UserId, t.ClientApplicationId })
            .HasDatabaseName("ix_refresh_tokens_user_app");

        builder.HasIndex(t => t.ExpiresAt).HasDatabaseName("ix_refresh_tokens_expires_at");
    }
}
