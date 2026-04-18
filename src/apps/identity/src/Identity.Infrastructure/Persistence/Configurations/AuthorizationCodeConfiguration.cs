using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class AuthorizationCodeConfiguration : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.ToTable("authorization_codes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.ClientApplicationId).IsRequired();

        builder.Property(c => c.CodeHash).IsRequired().HasMaxLength(64);

        builder.Property(c => c.RedirectUri).IsRequired().HasMaxLength(500);

        builder.Property(c => c.Scopes).HasColumnType("text[]").IsRequired();

        builder.Property(c => c.CodeChallenge).IsRequired().HasMaxLength(128);

        builder.Property(c => c.CodeChallengeMethod).IsRequired().HasMaxLength(10);

        builder.Property(c => c.State).HasMaxLength(500);

        builder.Property(c => c.SessionId).IsRequired().HasMaxLength(64);

        builder.Property(c => c.ExpiresAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(c => c.UsedAt).HasColumnType("timestamptz");

        builder.Property(c => c.CreatedAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(c => c.UpdatedAt).HasColumnType("timestamptz");

        builder
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.ClientApplication)
            .WithMany()
            .HasForeignKey(c => c.ClientApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Crítico — se usa en cada intercambio de code por token
        builder
            .HasIndex(c => c.CodeHash)
            .IsUnique()
            .HasDatabaseName("ix_authorization_codes_hash");

        builder.HasIndex(c => c.ExpiresAt).HasDatabaseName("ix_authorization_codes_expires_at");
    }
}
