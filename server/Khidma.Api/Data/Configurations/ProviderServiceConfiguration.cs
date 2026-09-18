using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ProviderServiceConfiguration : IEntityTypeConfiguration<ProviderService>
{
    public void Configure(EntityTypeBuilder<ProviderService> builder)
    {
        builder.HasKey(ps => ps.Id);

        builder.HasIndex(ps => new
        {
            ps.ProviderProfileId,
            ps.ServiceId
        })
        .IsUnique();

        builder.HasOne(ps => ps.ProviderProfile)
            .WithMany(p => p.ProviderServices)
            .HasForeignKey(ps => ps.ProviderProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Service)
            .WithMany(s => s.ProviderServices)
            .HasForeignKey(ps => ps.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
