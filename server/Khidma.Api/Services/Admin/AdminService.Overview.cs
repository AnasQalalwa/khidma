using Khidma.Api.Contracts.Admin;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Admin;

public sealed partial class AdminService
{
    private const string RangeToday = "today";
    private const string Range7d = "7d";
    private const string Range30d = "30d";
    private const string BucketHour = "hour";
    private const string BucketDay = "day";

    public async Task<ServiceResult<AdminOverviewDto>> GetOverviewAsync(
        string? range,
        CancellationToken cancellationToken)
    {
        if (!TryResolveRange(
                range,
                out var normalized,
                out var from,
                out var to,
                out var prevFrom,
                out var prevTo,
                out var bucket))
        {
            return ServiceResult<AdminOverviewDto>.Validation(
                "range",
                "Range must be today, 7d, or 30d.");
        }

        var now = to;
        var staleBefore = now.AddHours(-48);
        var securitySince = now.AddHours(-24);

        var bookingRows = await _db.Bookings
            .AsNoTracking()
            .Where(b =>
                (b.CreatedAt >= prevFrom && b.CreatedAt < to) ||
                (b.CompletedAt != null && b.CompletedAt >= prevFrom && b.CompletedAt < to) ||
                (b.CancelledAt != null && b.CancelledAt >= prevFrom && b.CancelledAt < to) ||
                b.Status == BookingStatus.Scheduled ||
                b.Status == BookingStatus.InProgress)
            .Select(b => new BookingRow(
                b.CreatedAt,
                b.CompletedAt,
                b.CancelledAt,
                b.ScheduledDate,
                b.Status,
                b.FinalPrice))
            .ToListAsync(cancellationToken);

        var requestRows = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= prevFrom && r.CreatedAt < to)
            .Select(r => new RequestRow(
                r.CreatedAt,
                r.Status,
                r.Offers.Any(o => o.Status != OfferStatus.Withdrawn)))
            .ToListAsync(cancellationToken);

        var reviewRows = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.CreatedAt >= prevFrom && r.CreatedAt < to)
            .Select(r => new ReviewRow(r.CreatedAt, r.Rating))
            .ToListAsync(cancellationToken);

        var pendingVerifications = await _db.ProviderProfiles.CountAsync(
            p => p.VerificationStatus == ProviderVerificationStatus.PendingReview,
            cancellationToken);

        var staleOpenRequests = await _db.ServiceRequests.CountAsync(
            r => r.Status == ServiceRequestStatus.Open &&
                 r.CreatedAt < staleBefore &&
                 !r.Offers.Any(o => o.Status != OfferStatus.Withdrawn),
            cancellationToken);

        var suspendedProviders = await _db.ProviderProfiles.CountAsync(
            p => p.IsSuspended,
            cancellationToken);

        var supplyRows = await LoadSupplyDemandAsync(cancellationToken);
        var topProviders = await _db.ProviderProfiles
            .AsNoTracking()
            .Select(p => new TopProviderDto
            {
                Id = p.Id,
                Name = p.User.FullName,
                City = p.City,
                Rating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                CompletedJobs = _db.Bookings.Count(b =>
                    b.ProviderId == p.UserId &&
                    b.Status == BookingStatus.Completed)
            })
            .OrderByDescending(p => p.Rating)
            .ThenByDescending(p => p.CompletedJobs)
            .Take(5)
            .ToListAsync(cancellationToken);

        var security = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.CreatedAt >= securitySince)
            .GroupBy(_ => 1)
            .Select(g => new Security24hDto
            {
                FailedLogins = g.Sum(a => a.Action == AuditActions.LoginFailed ? 1 : 0),
                DeniedActions = g.Sum(a =>
                    a.Outcome == AuditOutcomes.Denied &&
                    a.Action != AuditActions.CsrfRejected
                        ? 1
                        : 0),
                CsrfRejections = g.Sum(a => a.Action == AuditActions.CsrfRejected ? 1 : 0)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new Security24hDto
            {
                FailedLogins = 0,
                DeniedActions = 0,
                CsrfRejections = 0
            };

        var buckets = BuildBuckets(from, to, bucket);

        var currentRequests = requestRows.Where(r => InRange(r.CreatedAt, from, to)).ToList();
        var previousRequests = requestRows.Where(r => InRange(r.CreatedAt, prevFrom, prevTo)).ToList();

        var funnel = new FunnelDto
        {
            RequestsCreated = currentRequests.Count,
            RequestsWithOffer = currentRequests.Count(r => r.HasOffer),
            Booked = currentRequests.Count(ReachedBooked),
            Completed = currentRequests.Count(r => r.Status == ServiceRequestStatus.Completed)
        };

        var bookingValue = SumCompletedValue(bookingRows, from, to);
        var previousBookingValue = SumCompletedValue(bookingRows, prevFrom, prevTo);
        var bookingsCompleted = CountCompleted(bookingRows, from, to);
        var previousBookingsCompleted = CountCompleted(bookingRows, prevFrom, prevTo);
        var conversion = ConversionRate(currentRequests);
        var previousConversion = ConversionRate(previousRequests);
        var rating = AverageRating(reviewRows, from, to);
        var previousRating = AverageRating(reviewRows, prevFrom, prevTo);

        var overdueBookings = bookingRows.Count(b =>
            b.Status == BookingStatus.Scheduled &&
            b.ScheduledDate < now);

        return ServiceResult<AdminOverviewDto>.Success(new AdminOverviewDto
        {
            Range = normalized,
            From = from,
            To = to,
            Bucket = bucket,
            Kpis = new AdminKpisDto
            {
                BookingValue = new KpiDto
                {
                    Value = bookingValue,
                    Previous = previousBookingValue,
                    Series = buckets
                        .Select(date => new SeriesPointDto
                        {
                            Date = date,
                            Value = SumCompletedValue(
                                bookingRows,
                                date,
                                BucketEnd(date, bucket))
                        })
                        .ToList()
                },
                BookingsActive = new KpiDto
                {
                    Value = bookingRows.Count(b =>
                        b.Status == BookingStatus.Scheduled ||
                        b.Status == BookingStatus.InProgress),
                    Previous = null,
                    Series = buckets
                        .Select(date => new SeriesPointDto
                        {
                            Date = date,
                            Value = bookingRows.Count(b =>
                                (b.Status == BookingStatus.Scheduled ||
                                 b.Status == BookingStatus.InProgress) &&
                                InRange(b.ScheduledDate, date, BucketEnd(date, bucket)))
                        })
                        .ToList()
                },
                BookingsCompleted = new KpiDto
                {
                    Value = bookingsCompleted,
                    Previous = previousBookingsCompleted,
                    Series = buckets
                        .Select(date => new SeriesPointDto
                        {
                            Date = date,
                            Value = CountCompleted(
                                bookingRows,
                                date,
                                BucketEnd(date, bucket))
                        })
                        .ToList()
                },
                ConversionRate = new KpiDto
                {
                    Value = conversion,
                    Previous = previousConversion,
                    Series = buckets
                        .Select(date =>
                        {
                            var inBucket = requestRows
                                .Where(r => InRange(r.CreatedAt, date, BucketEnd(date, bucket)))
                                .ToList();
                            return new SeriesPointDto
                            {
                                Date = date,
                                Value = ConversionRate(inBucket)
                            };
                        })
                        .ToList()
                },
                AvgProviderRating = new KpiDto
                {
                    Value = rating,
                    Previous = previousRating,
                    Series = buckets
                        .Select(date => new SeriesPointDto
                        {
                            Date = date,
                            Value = AverageRating(
                                reviewRows,
                                date,
                                BucketEnd(date, bucket))
                        })
                        .ToList()
                }
            },
            BookingsSeries = buckets
                .Select(date =>
                {
                    var end = BucketEnd(date, bucket);
                    return new BookingsSeriesPointDto
                    {
                        Date = date,
                        Created = bookingRows.Count(b => InRange(b.CreatedAt, date, end)),
                        Completed = CountCompleted(bookingRows, date, end),
                        Cancelled = bookingRows.Count(b =>
                            InRange(b.CancelledAt, date, end))
                    };
                })
                .ToList(),
            Funnel = funnel,
            Attention = new AttentionCountsDto
            {
                PendingVerifications = pendingVerifications,
                StaleOpenRequests = staleOpenRequests,
                OverdueBookings = overdueBookings,
                SuspendedProviders = suspendedProviders
            },
            SupplyDemand = supplyRows,
            TopProviders = topProviders,
            Security24h = security
        });
    }

    private async Task<IReadOnlyList<SupplyDemandRowDto>> LoadSupplyDemandAsync(
        CancellationToken cancellationToken)
    {
        var openPairs = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Status == ServiceRequestStatus.Open)
            .Select(r => new
            {
                r.City,
                r.ServiceId,
                ServiceName = r.Service.Name
            })
            .ToListAsync(cancellationToken);

        var top = openPairs
            .GroupBy(row => (CityKey: row.City.ToLowerInvariant(), row.ServiceId))
            .Select(group => new
            {
                City = group.First().City,
                CityKey = group.Key.CityKey,
                group.Key.ServiceId,
                ServiceName = group.First().ServiceName,
                OpenRequests = group.Count()
            })
            .OrderByDescending(row => row.OpenRequests)
            .ThenBy(row => row.City)
            .Take(8)
            .ToList();

        if (top.Count == 0)
        {
            return [];
        }

        var serviceIds = top.Select(row => row.ServiceId).Distinct().ToList();
        var eligible = await _db.ProviderProfiles
            .AsNoTracking()
            .Where(p =>
                p.VerificationStatus == ProviderVerificationStatus.Approved &&
                !p.IsSuspended)
            .SelectMany(
                p => p.ProviderServices,
                (p, ps) => new { City = p.City.ToLower(), ps.ServiceId })
            .Where(row => serviceIds.Contains(row.ServiceId))
            .ToListAsync(cancellationToken);

        return top
            .Select(row => new SupplyDemandRowDto
            {
                City = row.City,
                ServiceId = row.ServiceId,
                ServiceName = row.ServiceName,
                OpenRequests = row.OpenRequests,
                EligibleProviders = eligible.Count(provider =>
                    provider.ServiceId == row.ServiceId &&
                    provider.City == row.CityKey)
            })
            .ToList();
    }

    private static bool TryResolveRange(
        string? range,
        out string normalized,
        out DateTimeOffset from,
        out DateTimeOffset to,
        out DateTimeOffset prevFrom,
        out DateTimeOffset prevTo,
        out string bucket)
    {
        normalized = range?.Trim().ToLowerInvariant() ?? string.Empty;
        var now = DateTimeOffset.UtcNow;
        switch (normalized)
        {
            case RangeToday:
                from = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
                to = now;
                prevFrom = from.AddDays(-1);
                prevTo = to.AddDays(-1);
                bucket = BucketHour;
                return true;
            case Range7d:
                to = now;
                from = now.AddDays(-7);
                prevTo = from;
                prevFrom = from.AddDays(-7);
                bucket = BucketDay;
                return true;
            case Range30d:
                to = now;
                from = now.AddDays(-30);
                prevTo = from;
                prevFrom = from.AddDays(-30);
                bucket = BucketDay;
                return true;
            default:
                from = default;
                to = default;
                prevFrom = default;
                prevTo = default;
                bucket = string.Empty;
                return false;
        }
    }

    private static IReadOnlyList<DateTimeOffset> BuildBuckets(
        DateTimeOffset from,
        DateTimeOffset to,
        string bucket)
    {
        var points = new List<DateTimeOffset>();
        if (bucket == BucketHour)
        {
            var cursor = new DateTimeOffset(
                from.UtcDateTime.Year,
                from.UtcDateTime.Month,
                from.UtcDateTime.Day,
                from.UtcDateTime.Hour,
                0,
                0,
                TimeSpan.Zero);
            var last = new DateTimeOffset(
                to.UtcDateTime.Year,
                to.UtcDateTime.Month,
                to.UtcDateTime.Day,
                to.UtcDateTime.Hour,
                0,
                0,
                TimeSpan.Zero);
            while (cursor <= last)
            {
                points.Add(cursor);
                cursor = cursor.AddHours(1);
            }

            return points;
        }

        var day = new DateTimeOffset(from.UtcDateTime.Date, TimeSpan.Zero);
        var lastDay = new DateTimeOffset(to.UtcDateTime.Date, TimeSpan.Zero);
        while (day <= lastDay)
        {
            points.Add(day);
            day = day.AddDays(1);
        }

        return points;
    }

    private static DateTimeOffset BucketEnd(DateTimeOffset start, string bucket) =>
        bucket == BucketHour ? start.AddHours(1) : start.AddDays(1);

    private static bool InRange(DateTimeOffset? value, DateTimeOffset start, DateTimeOffset end) =>
        value is { } timestamp && timestamp >= start && timestamp < end;

    private static decimal SumCompletedValue(
        IReadOnlyList<BookingRow> rows,
        DateTimeOffset start,
        DateTimeOffset end) =>
        rows
            .Where(row =>
                row.Status == BookingStatus.Completed &&
                InRange(row.CompletedAt, start, end))
            .Sum(row => row.FinalPrice);

    private static int CountCompleted(
        IReadOnlyList<BookingRow> rows,
        DateTimeOffset start,
        DateTimeOffset end) =>
        rows.Count(row =>
            row.Status == BookingStatus.Completed &&
            InRange(row.CompletedAt, start, end));

    private static decimal ConversionRate(IReadOnlyList<RequestRow> requests)
    {
        if (requests.Count == 0)
        {
            return 0m;
        }

        var converted = requests.Count(ReachedBooked);
        return Math.Round((decimal)converted / requests.Count, 4, MidpointRounding.AwayFromZero);
    }

    private static bool ReachedBooked(RequestRow request) =>
        request.Status is ServiceRequestStatus.Booked or ServiceRequestStatus.Completed;

    private static decimal AverageRating(
        IReadOnlyList<ReviewRow> rows,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var inRange = rows.Where(row => InRange(row.CreatedAt, start, end)).ToList();
        if (inRange.Count == 0)
        {
            return 0m;
        }

        return Math.Round(
            (decimal)inRange.Average(row => row.Rating),
            2,
            MidpointRounding.AwayFromZero);
    }

    private sealed record BookingRow(
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset ScheduledDate,
        BookingStatus Status,
        decimal FinalPrice);

    private sealed record RequestRow(
        DateTimeOffset CreatedAt,
        ServiceRequestStatus Status,
        bool HasOffer);

    private sealed record ReviewRow(DateTimeOffset CreatedAt, int Rating);
}
