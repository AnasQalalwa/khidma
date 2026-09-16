using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Dashboard;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Contracts.ServiceRequests;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.ServiceRequests;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IServiceRequestService _requests;

    public DashboardService(AppDbContext db, IServiceRequestService requests)
    {
        _db = db;
        _requests = requests;
    }

    public async Task<ServiceResult<CustomerDashboardDto>> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken)
    {
        var openRequestCount = await _db.ServiceRequests.CountAsync(
            r => r.CustomerId == customerId && r.Status == ServiceRequestStatus.Open,
            cancellationToken);

        var offersAwaitingDecision = await _db.Offers.CountAsync(
            o => o.ServiceRequest.CustomerId == customerId &&
                 o.Status == OfferStatus.Pending &&
                 o.ServiceRequest.Status == ServiceRequestStatus.Open,
            cancellationToken);

        var activeBookingCount = await _db.Bookings.CountAsync(
            b => b.CustomerId == customerId &&
                 (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InProgress),
            cancellationToken);

        var completedAwaitingReview = await _db.Bookings.CountAsync(
            b => b.CustomerId == customerId &&
                 b.Status == BookingStatus.Completed &&
                 b.Review == null,
            cancellationToken);

        var recentRequests = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.Id)
            .Take(5)
            .Select(r => new ServiceRequestSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                City = r.City,
                PreferredDate = r.PreferredDate,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                Status = r.Status.ToString(),
                OfferCount = r.Offers.Count(o => o.Status != OfferStatus.Withdrawn),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var activeBookings = await CustomerBookingQuery(customerId)
            .Where(b => b.Status == BookingStatus.Scheduled ||
                        b.Status == BookingStatus.InProgress)
            .OrderByDescending(b => b.Id)
            .Take(5)
            .Select(b => new BookingSummaryDto
            {
                Id = b.Id,
                ServiceRequestId = b.ServiceRequestId,
                OfferId = b.OfferId,
                Title = b.ServiceRequest.Title,
                ServiceName = b.ServiceRequest.Service.Name,
                CategoryName = b.ServiceRequest.Service.Category.Name,
                City = b.ServiceRequest.City,
                ScheduledDate = b.ScheduledDate,
                FinalPrice = b.FinalPrice,
                Status = b.Status.ToString(),
                CounterpartyName = b.Provider.FullName,
                CreatedAt = b.CreatedAt,
                HasReview = b.Review != null
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<CustomerDashboardDto>.Success(new CustomerDashboardDto
        {
            OpenRequestCount = openRequestCount,
            OffersAwaitingDecision = offersAwaitingDecision,
            ActiveBookingCount = activeBookingCount,
            CompletedAwaitingReview = completedAwaitingReview,
            RecentRequests = recentRequests,
            ActiveBookings = activeBookings
        });
    }

    public async Task<ServiceResult<ProviderDashboardDto>> GetProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var profile = await _requests.FindProviderProfileAsync(
            providerUserId,
            cancellationToken);
        if (profile is null)
        {
            return ServiceResult<ProviderDashboardDto>.NotFound("Provider profile not found.");
        }

        var eligibleQuery = _requests.EligibleOpenRequestsForProvider(profile);
        var eligibleRequestCount = await eligibleQuery.CountAsync(cancellationToken);

        var pendingOfferCount = await _db.Offers.CountAsync(
            o => o.ProviderId == providerUserId && o.Status == OfferStatus.Pending,
            cancellationToken);

        var activeBookingCount = await _db.Bookings.CountAsync(
            b => b.ProviderId == providerUserId &&
                 (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InProgress),
            cancellationToken);

        var recentAvailable = await eligibleQuery
            .AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Take(5)
            .Select(r => new RequestSummaryForProviderDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                City = r.City,
                PreferredDate = r.PreferredDate,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentOffers = await _db.Offers
            .AsNoTracking()
            .Where(o => o.ProviderId == providerUserId)
            .OrderByDescending(o => o.Id)
            .Take(5)
            .Select(o => new OfferMineDto
            {
                Id = o.Id,
                ServiceRequestId = o.ServiceRequestId,
                RequestTitle = o.ServiceRequest.Title,
                ServiceName = o.ServiceRequest.Service.Name,
                CategoryName = o.ServiceRequest.Service.Category.Name,
                City = o.ServiceRequest.City,
                Price = o.Price,
                Message = o.Message,
                EstimatedDate = o.EstimatedDate,
                Status = o.Status.ToString(),
                RequestStatus = o.ServiceRequest.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var activeJobs = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.ProviderId == providerUserId &&
                        (b.Status == BookingStatus.Scheduled ||
                         b.Status == BookingStatus.InProgress))
            .OrderByDescending(b => b.Id)
            .Take(5)
            .Select(b => new BookingSummaryDto
            {
                Id = b.Id,
                ServiceRequestId = b.ServiceRequestId,
                OfferId = b.OfferId,
                Title = b.ServiceRequest.Title,
                ServiceName = b.ServiceRequest.Service.Name,
                CategoryName = b.ServiceRequest.Service.Category.Name,
                City = b.ServiceRequest.City,
                ScheduledDate = b.ScheduledDate,
                FinalPrice = b.FinalPrice,
                Status = b.Status.ToString(),
                CounterpartyName = b.Customer.FullName,
                CreatedAt = b.CreatedAt,
                HasReview = b.Review != null
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<ProviderDashboardDto>.Success(new ProviderDashboardDto
        {
            IsApproved = profile.IsApproved,
            EligibleRequestCount = eligibleRequestCount,
            PendingOfferCount = pendingOfferCount,
            ActiveBookingCount = activeBookingCount,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            RecentAvailableRequests = recentAvailable,
            RecentOffers = recentOffers,
            ActiveJobs = activeJobs
        });
    }

    private IQueryable<Domain.Booking> CustomerBookingQuery(string customerId) =>
        _db.Bookings.AsNoTracking().Where(b => b.CustomerId == customerId);
}
