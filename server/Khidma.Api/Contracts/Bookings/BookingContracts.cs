using System.ComponentModel.DataAnnotations;
using Khidma.Api.Contracts.Reviews;

namespace Khidma.Api.Contracts.Bookings;

public sealed class CancelBookingRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = default!;
}

public sealed class BookingSummaryDto
{
    public required int Id { get; init; }

    public required int ServiceRequestId { get; init; }

    public required int OfferId { get; init; }

    public required string Title { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset ScheduledDate { get; init; }

    public required decimal FinalPrice { get; init; }

    public required string Status { get; init; }

    public required string CounterpartyName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required bool HasReview { get; init; }
}

public sealed class BookingDetailDto
{
    public required int Id { get; init; }

    public required int ServiceRequestId { get; init; }

    public required int OfferId { get; init; }

    public required int ProviderProfileId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset ScheduledDate { get; init; }

    public required decimal FinalPrice { get; init; }

    public required string Status { get; init; }

    public required string CustomerId { get; init; }

    public required string ProviderId { get; init; }

    public required string CustomerName { get; init; }

    public required string ProviderName { get; init; }

    public string? CustomerEmail { get; init; }

    public string? ProviderEmail { get; init; }

    public string? CustomerContact { get; init; }

    public string? CustomerCity { get; init; }

    public string? ProviderCity { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public string? CancellationReason { get; init; }

    public required bool CanStart { get; init; }

    public required bool CanComplete { get; init; }

    public required bool CanCancel { get; init; }

    public required bool CanReview { get; init; }

    public ReviewDto? Review { get; init; }
}
