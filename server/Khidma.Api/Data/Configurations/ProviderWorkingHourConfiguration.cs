using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khidma.Api.Data.Configurations;

public class ProviderWorkingHourConfiguration : IEntityTypeConfiguration<ProviderWorkingHour>
{
    public void Configure(EntityTypeBuilder<ProviderWorkingHour> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasIndex(h => new { h.ProviderProfileId, h.DayOfWeek, h.Hour })
            .IsUnique();

        builder.HasOne(h => h.ProviderProfile)
            .WithMany(p => p.WorkingHours)
            .HasForeignKey(h => h.ProviderProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ProviderWorkingHours_DayAndHour",
            "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6 AND [Hour] >= 0 AND [Hour] <= 23"));
    }
}
