using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CustomerId)
            .IsRequired();

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.City)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(r => r.BudgetMin)
            .HasPrecision(18, 2);

        builder.Property(r => r.BudgetMax)
            .HasPrecision(18, 2);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Service)
            .WithMany(s => s.ServiceRequests)
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new
        {
            r.Status,
            r.ServiceId,
            r.City
        });

        builder.HasIndex(r => new
        {
            r.CustomerId,
            r.Status
        });
    }
}
