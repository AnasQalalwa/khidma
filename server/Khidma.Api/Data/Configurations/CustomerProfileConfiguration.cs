using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.City)
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasOne(p => p.User)
            .WithOne(u => u.CustomerProfile)
            .HasForeignKey<CustomerProfile>(p => p.UserId);
    }
}
