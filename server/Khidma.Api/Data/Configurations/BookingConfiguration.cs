using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.CustomerId)
            .IsRequired();

        builder.Property(b => b.ProviderId)
            .IsRequired();

        builder.Property(b => b.FinalPrice)
            .HasPrecision(18, 2);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.HasOne(b => b.Offer)
            .WithOne(o => o.Booking)
            .HasForeignKey<Booking>(b => b.OfferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ServiceRequest)
            .WithMany()
            .HasForeignKey(b => b.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Provider)
            .WithMany()
            .HasForeignKey(b => b.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.OfferId)
            .IsUnique();

        builder.HasIndex(b => new
        {
            b.ProviderId,
            b.Status
        });

        builder.HasIndex(b => new
        {
            b.CustomerId,
            b.Status
        });
    }
}
