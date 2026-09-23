using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(c => c.ImageStoredFileName)
            .HasMaxLength(260);

        builder.Property(c => c.ImageContentType)
            .HasMaxLength(100);

        builder.Ignore(c => c.HasImage);
    }
}
