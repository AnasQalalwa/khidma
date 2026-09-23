namespace Khidma.Api.Domain;

public class ProviderWorkingHour
{
    public int Id { get; set; }

    public int ProviderProfileId { get; set; }

    /// <summary>Sunday = 0 through Saturday = 6.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Hour of the day, 0 through 23, in the provider's local time.</summary>
    public int Hour { get; set; }

    public ProviderProfile ProviderProfile { get; set; } = default!;
}
