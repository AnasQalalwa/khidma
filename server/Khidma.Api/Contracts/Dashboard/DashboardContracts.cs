using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Contracts.ServiceRequests;

namespace Khidma.Api.Contracts.Dashboard;

public sealed class CustomerDashboardDto
{
    public required int OpenRequestCount { get; init; }

    public required int OffersAwaitingDecision { get; init; }

    public required int ActiveBookingCount { get; init; }

    public required int CompletedAwaitingReview { get; init; }

    public required IReadOnlyList<ServiceRequestSummaryDto> RecentRequests { get; init; }

    public required IReadOnlyList<BookingSummaryDto> ActiveBookings { get; init; }
}

public sealed class ProviderDashboardDto
{
    public required bool IsApproved { get; init; }

    public required int EligibleRequestCount { get; init; }

    public required int PendingOfferCount { get; init; }

    public required int ActiveBookingCount { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<RequestSummaryForProviderDto> RecentAvailableRequests { get; init; }

    public required IReadOnlyList<OfferMineDto> RecentOffers { get; init; }

    public required IReadOnlyList<BookingSummaryDto> ActiveJobs { get; init; }
}
