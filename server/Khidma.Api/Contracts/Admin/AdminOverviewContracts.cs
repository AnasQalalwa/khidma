namespace Khidma.Api.Contracts.Admin;

public sealed class AdminOverviewDto
{
    public required string Range { get; init; }

    public required DateTimeOffset From { get; init; }

    public required DateTimeOffset To { get; init; }

    public required string Bucket { get; init; }

    public required AdminKpisDto Kpis { get; init; }

    public required IReadOnlyList<BookingsSeriesPointDto> BookingsSeries { get; init; }

    public required FunnelDto Funnel { get; init; }

    public required AttentionCountsDto Attention { get; init; }

    public required IReadOnlyList<SupplyDemandRowDto> SupplyDemand { get; init; }

    public required IReadOnlyList<TopProviderDto> TopProviders { get; init; }

    public required Security24hDto Security24h { get; init; }
}

public sealed class AdminKpisDto
{
    public required KpiDto BookingValue { get; init; }

    public required KpiDto BookingsActive { get; init; }

    public required KpiDto BookingsCompleted { get; init; }

    public required KpiDto ConversionRate { get; init; }

    public required KpiDto AvgProviderRating { get; init; }
}

public sealed class KpiDto
{
    public required decimal Value { get; init; }

    public decimal? Previous { get; init; }

    public required IReadOnlyList<SeriesPointDto> Series { get; init; }
}

public sealed class SeriesPointDto
{
    public required DateTimeOffset Date { get; init; }

    public required decimal Value { get; init; }
}

public sealed class BookingsSeriesPointDto
{
    public required DateTimeOffset Date { get; init; }

    public required int Created { get; init; }

    public required int Completed { get; init; }

    public required int Cancelled { get; init; }
}

public sealed class FunnelDto
{
    public required int RequestsCreated { get; init; }

    public required int RequestsWithOffer { get; init; }

    public required int Booked { get; init; }

    public required int Completed { get; init; }
}

public sealed class AttentionCountsDto
{
    public required int PendingVerifications { get; init; }

    public required int StaleOpenRequests { get; init; }

    public required int OverdueBookings { get; init; }

    public required int SuspendedProviders { get; init; }
}

public sealed class SupplyDemandRowDto
{
    public required string City { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required int OpenRequests { get; init; }

    public required int EligibleProviders { get; init; }
}

public sealed class TopProviderDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string City { get; init; }

    public required decimal Rating { get; init; }

    public required int ReviewCount { get; init; }

    public required int CompletedJobs { get; init; }
}

public sealed class Security24hDto
{
    public required int FailedLogins { get; init; }

    public required int DeniedActions { get; init; }

    public required int CsrfRejections { get; init; }
}
