using Khidma.Api.Auth;
using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Reviews;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khidma.Api.Services.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingService> _logger;
    private readonly IAuditService _audit;

    public BookingService(AppDbContext db, ILogger<BookingService> logger, IAuditService audit)
    {
        _db = db;
        _logger = logger;
        _audit = audit;
    }

    public async Task<ServiceResult<PagedResult<BookingSummaryDto>>> GetMineAsync(
        string userId,
        string role,
        PageQuery paging,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = paging.Normalize();
        var isProvider = role == AppRoles.Provider;
        var query = _db.Bookings.AsNoTracking();

        query = isProvider
            ? query.Where(b => b.ProviderId == userId)
            : query.Where(b => b.CustomerId == userId);

        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            if (!BookingStatusParser.TryParse(paging.Status, out var status))
            {
                return ServiceResult<PagedResult<BookingSummaryDto>>.Validation(
                    "status",
                    "Status is not valid.");
            }

            query = query.Where(b => b.Status == status);
        }

        var pageResult = await query
            .OrderByDescending(b => b.Id)
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
                CounterpartyName = isProvider
                    ? b.Customer.FullName
                    : b.Provider.FullName,
                CreatedAt = b.CreatedAt,
                HasReview = b.Review != null
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<BookingSummaryDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<BookingDetailDto>> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var booking = await DetailQuery()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        var isCustomer = booking.CustomerId == userId;
        var isProvider = booking.ProviderId == userId;
        if (!isAdmin && !isCustomer && !isProvider)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "You are not a participant in this booking.");
        }

        return ServiceResult<BookingDetailDto>.Success(
            ToDetail(booking, isCustomer, isProvider));
    }

    public async Task<ServiceResult<BookingDetailDto>> StartAsync(
        int id,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == id,
            cancellationToken);

        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        if (booking.ProviderId != providerUserId)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "Only the assigned provider can start this booking.");
        }

        if (booking.Status != BookingStatus.Scheduled)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "Only scheduled bookings can be started.");
        }

        booking.Status = BookingStatus.InProgress;
        booking.StartedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provider {UserId} started booking {BookingId}",
            providerUserId,
            booking.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingStarted,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Provider started a booking."
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, providerUserId, isAdmin: false, cancellationToken);
    }

    public async Task<ServiceResult<BookingDetailDto>> CompleteAsync(
        int id,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var booking = await _db.Bookings.FirstOrDefaultAsync(
                    b => b.Id == id,
                    cancellationToken);

                if (booking is null)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.NotFound("Booking not found."),
                        cancellationToken);
                }

                if (booking.ProviderId != providerUserId)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.Forbidden(
                            "Only the assigned provider can complete this booking."),
                        cancellationToken);
                }

                if (booking.Status != BookingStatus.InProgress)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.Conflict(
                            "Only in-progress bookings can be completed."),
                        cancellationToken);
                }

                var request = await _db.ServiceRequests.FirstAsync(
                    r => r.Id == booking.ServiceRequestId,
                    cancellationToken);

                booking.Status = BookingStatus.Completed;
                booking.CompletedAt = DateTimeOffset.UtcNow;
                request.Status = ServiceRequestStatus.Completed;

                await _db.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Provider {UserId} completed booking {BookingId} and request {ServiceRequestId}",
                    providerUserId,
                    booking.Id,
                    request.Id);

                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Booking,
                    Action = AuditActions.BookingCompleted,
                    Outcome = AuditOutcomes.Success,
                    EntityType = nameof(Booking),
                    EntityId = booking.Id.ToString(),
                    Message = "Provider completed a booking.",
                    Details = new Dictionary<string, object?>
                    {
                        ["serviceRequestId"] = request.Id
                    }
                }, cancellationToken);

                return await GetByIdAsync(
                    booking.Id,
                    providerUserId,
                    isAdmin: false,
                    cancellationToken);
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        });
    }

    public async Task<ServiceResult<BookingDetailDto>> CancelAsync(
        int id,
        string userId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "reason",
                "A cancellation reason is required.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var booking = await _db.Bookings.FirstOrDefaultAsync(
                    b => b.Id == id,
                    cancellationToken);

                if (booking is null)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.NotFound("Booking not found."),
                        cancellationToken);
                }

                if (booking.CustomerId != userId && booking.ProviderId != userId)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.Forbidden(
                            "You are not a participant in this booking."),
                        cancellationToken);
                }

                if (booking.Status != BookingStatus.Scheduled)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.Conflict(
                            "Only scheduled bookings can be cancelled."),
                        cancellationToken);
                }

                var request = await _db.ServiceRequests.FirstAsync(
                    r => r.Id == booking.ServiceRequestId,
                    cancellationToken);

                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = DateTimeOffset.UtcNow;
                booking.CancellationReason = reason.Trim();
                request.Status = ServiceRequestStatus.Cancelled;

                await _db.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "User {UserId} cancelled booking {BookingId} and request {ServiceRequestId}",
                    userId,
                    booking.Id,
                    request.Id);

                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Booking,
                    Action = AuditActions.BookingCancelled,
                    Outcome = AuditOutcomes.Success,
                    EntityType = nameof(Booking),
                    EntityId = booking.Id.ToString(),
                    Message = "Booking cancelled.",
                    Details = new Dictionary<string, object?>
                    {
                        ["serviceRequestId"] = request.Id
                    }
                }, cancellationToken);

                return await GetByIdAsync(booking.Id, userId, isAdmin: false, cancellationToken);
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        });
    }

    private IQueryable<Booking> DetailQuery() =>
        _db.Bookings
            .AsNoTracking()
            .Include(b => b.ServiceRequest)
                .ThenInclude(r => r.Service)
                    .ThenInclude(s => s.Category)
            .Include(b => b.Customer)
                .ThenInclude(c => c.CustomerProfile)
            .Include(b => b.Provider)
                .ThenInclude(p => p.ProviderProfile)
            .Include(b => b.Review);

    private static BookingDetailDto ToDetail(
        Booking booking,
        bool isCustomer,
        bool isProvider)
    {
        var review = booking.Review is null
            ? null
            : new ReviewDto
            {
                Id = booking.Review.Id,
                Rating = booking.Review.Rating,
                Comment = booking.Review.Comment,
                CreatedAt = booking.Review.CreatedAt,
                CustomerDisplayName = booking.Customer.FullName
            };

        return new BookingDetailDto
        {
            Id = booking.Id,
            ServiceRequestId = booking.ServiceRequestId,
            OfferId = booking.OfferId,
            ProviderProfileId = booking.Provider.ProviderProfile?.Id ?? 0,
            Title = booking.ServiceRequest.Title,
            Description = booking.ServiceRequest.Description,
            ServiceName = booking.ServiceRequest.Service.Name,
            CategoryName = booking.ServiceRequest.Service.Category.Name,
            City = booking.ServiceRequest.City,
            ScheduledDate = booking.ScheduledDate,
            FinalPrice = booking.FinalPrice,
            Status = booking.Status.ToString(),
            CustomerId = booking.CustomerId,
            ProviderId = booking.ProviderId,
            CustomerName = booking.Customer.FullName,
            ProviderName = booking.Provider.FullName,
            CustomerEmail = booking.Customer.Email,
            ProviderEmail = booking.Provider.Email,
            CustomerContact = booking.Customer.CustomerProfile?.DefaultContact,
            CustomerCity = booking.Customer.CustomerProfile?.City,
            ProviderCity = booking.Provider.ProviderProfile?.City,
            CreatedAt = booking.CreatedAt,
            StartedAt = booking.StartedAt,
            CompletedAt = booking.CompletedAt,
            CancelledAt = booking.CancelledAt,
            CancellationReason = booking.CancellationReason,
            CanStart = isProvider && booking.Status == BookingStatus.Scheduled,
            CanComplete = isProvider && booking.Status == BookingStatus.InProgress,
            CanCancel = (isCustomer || isProvider) &&
                        booking.Status == BookingStatus.Scheduled,
            CanReview = isCustomer &&
                        booking.Status == BookingStatus.Completed &&
                        booking.Review is null,
            Review = review
        };
    }

    private static async Task<ServiceResult<T>> Abort<T>(
        IDbContextTransaction? transaction,
        ServiceResult<T> result,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        return result;
    }
}
