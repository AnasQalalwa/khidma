namespace Khidma.Api.Domain;

public class Review
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string CustomerId { get; set; } = default!;

    public string ProviderId { get; set; } = default!;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Booking Booking { get; set; } = default!;

    public ApplicationUser Customer { get; set; } = default!;

    public ApplicationUser Provider { get; set; } = default!;
}
