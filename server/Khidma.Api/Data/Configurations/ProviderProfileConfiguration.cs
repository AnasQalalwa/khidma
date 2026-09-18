using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ProviderProfileConfiguration : IEntityTypeConfiguration<ProviderProfile>
{
    public void Configure(EntityTypeBuilder<ProviderProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.CanReceiveWork);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.City)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(p => p.Bio)
            .HasMaxLength(1000);

        builder.Property(p => p.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.VerificationReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(p => p.VerificationRejectionReason)
            .HasMaxLength(1000);

        builder.Property(p => p.SuspensionReason)
            .HasMaxLength(500);

        builder.Property(p => p.SuspendedByUserId)
            .HasMaxLength(450);

        builder.Property(p => p.AverageRating)
            .HasPrecision(3, 2);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasIndex(p => p.VerificationStatus);

        builder.HasIndex(p => p.IsSuspended);

        builder.HasOne(p => p.User)
            .WithOne(u => u.ProviderProfile)
            .HasForeignKey<ProviderProfile>(p => p.UserId);
    }
}
