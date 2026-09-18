using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ProviderVerificationDocumentConfiguration
    : IEntityTypeConfiguration<ProviderVerificationDocument>
{
    public void Configure(EntityTypeBuilder<ProviderVerificationDocument> builder)
    {
        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_ProviderVerificationDocuments_FileSize",
                "FileSizeBytes > 0"));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(d => d.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(d => d.StoredFileName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.ReviewStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.ReviewNote)
            .HasMaxLength(1000);

        builder.Property(d => d.ReviewedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(d => d.StoredFileName)
            .IsUnique();

        builder.HasIndex(d => new { d.ProviderProfileId, d.ReviewStatus });

        builder.HasOne(d => d.ProviderProfile)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProviderProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
