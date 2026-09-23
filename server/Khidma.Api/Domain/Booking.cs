using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class Booking
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = default!;

    public string ProviderId { get; set; } = default!;

    public int ServiceId { get; set; }

    public string City { get; set; } = default!;

    public string? Notes { get; set; }

    public DateOnly RequestedDate { get; set; }

    public DateTimeOffset? ScheduledStart { get; set; }

    public int? DurationHours { get; set; }

    public DateTimeOffset? RescheduledAt { get; set; }

    public string? RescheduleNote { get; set; }

    public BookingStatus Status { get; set; }

    public decimal? QuotedPrice { get; set; }

    public string? ProviderMessage { get; set; }

    public string? DeclineReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public byte[] RowVersion { get; set; } = default!;

    public Service Service { get; set; } = default!;

    public ApplicationUser Customer { get; set; } = default!;

    public ApplicationUser Provider { get; set; } = default!;

    public Review? Review { get; set; }
}
