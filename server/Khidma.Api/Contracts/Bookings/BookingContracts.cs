using System.ComponentModel.DataAnnotations;
using Khidma.Api.Contracts.Reviews;

namespace Khidma.Api.Contracts.Bookings;

public sealed class CreateBookingRequest
{
    [Required]
    public int ProviderProfileId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    [Required]
    public DateOnly RequestedDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed class AcceptBookingRequest
{
    [Required]
    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Price { get; set; }

    [StringLength(1000)]
    public string? Message { get; set; }

    public DateTimeOffset ScheduledStart { get; set; }

    public int DurationHours { get; set; }
}

public sealed class RescheduleBookingRequest
{
    public DateTimeOffset ScheduledStart { get; set; }

    public int DurationHours { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public sealed class DeclineBookingRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = default!;
}

public sealed class CancelBookingRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = default!;
}

public sealed class BookingSummaryDto
{
    public required int Id { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateOnly RequestedDate { get; init; }

    public DateTimeOffset? ScheduledStart { get; init; }

    public DateTimeOffset? ScheduledEnd { get; set; }

    public int? DurationHours { get; init; }

    public decimal? QuotedPrice { get; init; }

    public required string Status { get; init; }

    public required string CounterpartyName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required bool HasReview { get; init; }
}

public sealed class BookingDetailDto
{
    public required int Id { get; init; }

    public required int ServiceId { get; init; }

    public required int ProviderProfileId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public string? Notes { get; init; }

    public required DateOnly RequestedDate { get; init; }

    public DateTimeOffset? ScheduledStart { get; init; }

    public DateTimeOffset? ScheduledEnd { get; init; }

    public int? DurationHours { get; init; }

    public DateTimeOffset? RescheduledAt { get; init; }

    public string? RescheduleNote { get; init; }

    public decimal? QuotedPrice { get; init; }

    public string? ProviderMessage { get; init; }

    public string? DeclineReason { get; init; }

    public required string Status { get; init; }

    public required string CustomerId { get; init; }

    public required string ProviderId { get; init; }

    public required string CustomerName { get; init; }

    public required string ProviderName { get; init; }

    public string? CustomerPhone { get; init; }

    public string? ProviderPhone { get; init; }

    public string? CustomerCity { get; init; }

    public string? ProviderCity { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? RespondedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public string? CancellationReason { get; init; }

    public required bool CanAccept { get; init; }

    public required bool CanReschedule { get; init; }

    public required bool CanDecline { get; init; }

    public required bool CanStart { get; init; }

    public required bool CanComplete { get; init; }

    public required bool CanCancel { get; init; }

    public required bool CanReview { get; init; }

    public ReviewDto? Review { get; init; }
}
