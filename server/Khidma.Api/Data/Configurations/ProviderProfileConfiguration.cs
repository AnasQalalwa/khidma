using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ProviderProfileConfiguration : IEntityTypeConfiguration<ProviderProfile>
{
    public void Configure(EntityTypeBuilder<ProviderProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.City)
            .IsRequired();

        builder.Property(p => p.AverageRating)
            .HasPrecision(3, 2);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasOne(p => p.User)
            .WithOne(u => u.ProviderProfile)
            .HasForeignKey<ProviderProfile>(p => p.UserId);
    }
}

