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

        builder.Property(b => b.City)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(b => b.Notes)
            .HasMaxLength(1000);

        builder.Property(b => b.QuotedPrice)
            .HasPrecision(18, 2);

        builder.Property(b => b.ProviderMessage)
            .HasMaxLength(1000);

        builder.Property(b => b.DeclineReason)
            .HasMaxLength(500);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.RescheduleNote)
            .HasMaxLength(500);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Bookings_DurationHours",
            "[DurationHours] IS NULL OR ([DurationHours] >= 1 AND [DurationHours] <= 12)"));

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.HasOne(b => b.Service)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Provider)
            .WithMany()
            .HasForeignKey(b => b.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new
            {
                b.CustomerId,
                b.ProviderId,
                b.ServiceId
            })
            .IsUnique()
            .HasFilter("[Status] = 'Pending'");

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
