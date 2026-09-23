using Khidma.Api.Contracts.Bookings;

namespace Khidma.Api.Contracts.Dashboard;

public sealed class ProviderDashboardDto
{
    public required string VerificationStatus { get; init; }

    public required bool IsSuspended { get; init; }

    public string? SuspensionReason { get; init; }

    public required int PendingRequestCount { get; init; }

    public required int ActiveJobCount { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<BookingSummaryDto> RecentPendingRequests { get; init; }

    public required IReadOnlyList<BookingSummaryDto> ActiveJobs { get; init; }
}
