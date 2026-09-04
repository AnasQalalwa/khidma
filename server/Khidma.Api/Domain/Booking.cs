using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class Booking
{
    public int Id { get; set; }

    public int OfferId { get; set; }

    public int ServiceRequestId { get; set; }

    public string CustomerId { get; set; } = default!;

    public string ProviderId { get; set; } = default!;

    public DateTimeOffset ScheduledDate { get; set; }

    public decimal FinalPrice { get; set; }

    public BookingStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public byte[] RowVersion { get; set; } = default!;

    public Offer Offer { get; set; } = default!;

    public ServiceRequest ServiceRequest { get; set; } = default!;

    public ApplicationUser Customer { get; set; } = default!;

    public ApplicationUser Provider { get; set; } = default!;

    public Review? Review { get; set; }
}
