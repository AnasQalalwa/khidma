using System.Linq.Expressions;
using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Dashboard;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ProviderDashboardDto>> GetProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == providerUserId, cancellationToken);
        if (profile is null)
        {
            return ServiceResult<ProviderDashboardDto>.NotFound("Provider profile not found.");
        }

        var pendingRequestCount = await _db.Bookings.CountAsync(
            b => b.ProviderId == providerUserId && b.Status == BookingStatus.Pending,
            cancellationToken);

        var activeJobCount = await _db.Bookings.CountAsync(
            b => b.ProviderId == providerUserId &&
                 (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InProgress),
            cancellationToken);

        var recentPending = await Summaries(
                providerUserId,
                b => b.Status == BookingStatus.Pending)
            .OrderByDescending(b => b.Id)
            .Take(5)
            .ToListAsync(cancellationToken);

        var activeJobs = await Summaries(
                providerUserId,
                b => b.Status == BookingStatus.Scheduled ||
                     b.Status == BookingStatus.InProgress)
            .OrderByDescending(b => b.Id)
            .Take(5)
            .ToListAsync(cancellationToken);

        FillEnds(recentPending);
        FillEnds(activeJobs);

        return ServiceResult<ProviderDashboardDto>.Success(new ProviderDashboardDto
        {
            VerificationStatus = profile.VerificationStatus.ToString(),
            IsSuspended = profile.IsSuspended,
            SuspensionReason = profile.SuspensionReason,
            PendingRequestCount = pendingRequestCount,
            ActiveJobCount = activeJobCount,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            RecentPendingRequests = recentPending,
            ActiveJobs = activeJobs
        });
    }

    private static void FillEnds(IEnumerable<BookingSummaryDto> items)
    {
        foreach (var item in items)
        {
            if (item.ScheduledStart is not null && item.DurationHours is not null)
            {
                item.ScheduledEnd = item.ScheduledStart.Value.AddHours(item.DurationHours.Value);
            }
        }
    }

    private IQueryable<BookingSummaryDto> Summaries(
        string providerUserId,
        Expression<Func<Booking, bool>> predicate) =>
        _db.Bookings
            .AsNoTracking()
            .Where(b => b.ProviderId == providerUserId)
            .Where(predicate)
            .Select(b => new BookingSummaryDto
            {
                Id = b.Id,
                ServiceId = b.ServiceId,
                ServiceName = b.Service.Name,
                CategoryName = b.Service.Category.Name,
                City = b.City,
                RequestedDate = b.RequestedDate,
                ScheduledStart = b.ScheduledStart,
                DurationHours = b.DurationHours,
                QuotedPrice = b.QuotedPrice,
                Status = b.Status.ToString(),
                CounterpartyName = b.Customer.FullName,
                CreatedAt = b.CreatedAt,
                HasReview = b.Review != null
            });
}
