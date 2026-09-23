using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ProviderProfileChangeRequestConfiguration
    : IEntityTypeConfiguration<ProviderProfileChangeRequest>
{
    public void Configure(EntityTypeBuilder<ProviderProfileChangeRequest> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.RequestedCity)
            .HasMaxLength(80);

        builder.Property(c => c.RequestedLatitude)
            .HasPrecision(9, 6);

        builder.Property(c => c.RequestedLongitude)
            .HasPrecision(9, 6);

        builder.Property(c => c.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(c => c.ReviewNote)
            .HasMaxLength(1000);

        builder.HasIndex(c => c.ProviderProfileId);

        builder.HasIndex(c => c.ProviderProfileId)
            .HasFilter("[Status] = 'Pending' AND [Type] = 'Location'")
            .IsUnique()
            .HasDatabaseName("UX_ProviderChange_PendingLocation");

        builder.HasIndex(c => new { c.ProviderProfileId, c.ServiceId })
            .HasFilter("[Status] = 'Pending' AND [Type] = 'AddService' AND [ServiceId] IS NOT NULL")
            .IsUnique()
            .HasDatabaseName("UX_ProviderChange_PendingService");

        builder.HasOne(c => c.ProviderProfile)
            .WithMany(p => p.ChangeRequests)
            .HasForeignKey(c => c.ProviderProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Service)
            .WithMany()
            .HasForeignKey(c => c.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ProofDocument)
            .WithMany()
            .HasForeignKey(c => c.ProofDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
