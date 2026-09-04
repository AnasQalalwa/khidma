using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ProviderId)
            .IsRequired();

        builder.Property(o => o.Price)
            .HasPrecision(18, 2);

        builder.Property(o => o.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(o => o.ServiceRequest)
            .WithMany(r => r.Offers)
            .HasForeignKey(o => o.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Provider)
            .WithMany()
            .HasForeignKey(o => o.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.ServiceRequestId)
            .IsUnique()
            .HasFilter("[Status] = 'Accepted'")
            .HasDatabaseName("UX_Offer_OneAcceptedPerRequest");

        builder.HasIndex(o => new
        {
            o.ServiceRequestId,
            o.ProviderId
        })
        .IsUnique()
        .HasFilter("[Status] <> 'Withdrawn'");

        builder.HasIndex(o => new
        {
            o.ProviderId,
            o.Status
        });
    }
}
