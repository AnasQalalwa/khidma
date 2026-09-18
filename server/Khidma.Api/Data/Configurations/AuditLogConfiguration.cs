using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorUserId)
            .HasMaxLength(450);

        builder.Property(a => a.ActorEmail)
            .HasMaxLength(256);

        builder.Property(a => a.ActorRole)
            .HasMaxLength(50);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .HasMaxLength(100);

        builder.Property(a => a.Outcome)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Message)
            .HasMaxLength(1000);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(64);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(512);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(64);

        builder.HasIndex(a => a.CreatedAt);

        builder.HasIndex(a => new { a.ActorUserId, a.CreatedAt });

        builder.HasIndex(a => new { a.Category, a.CreatedAt });

        builder.HasIndex(a => new { a.Action, a.CreatedAt });

        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        builder.HasIndex(a => new { a.Outcome, a.CreatedAt });
    }
}
