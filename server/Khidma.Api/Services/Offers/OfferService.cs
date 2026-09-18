using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Infrastructure;
using Khidma.Api.Services.Audit;
using Khidma.Api.Services.Bookings;
using Khidma.Api.Services.ServiceRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khidma.Api.Services.Offers;

public sealed class OfferService : IOfferService
{
    public const string AnotherOfferAcceptedFirst = "Another offer was accepted first.";

    private readonly AppDbContext _db;
    private readonly IServiceRequestService _requests;
    private readonly IBookingService _bookings;
    private readonly ILogger<OfferService> _logger;
    private readonly IAuditService _audit;

    public OfferService(
        AppDbContext db,
        IServiceRequestService requests,
        IBookingService bookings,
        ILogger<OfferService> logger,
        IAuditService audit)
    {
        _db = db;
        _requests = requests;
        _bookings = bookings;
        _logger = logger;
        _audit = audit;
    }

    public async Task<ServiceResult<OfferSnapshotDto>> SubmitAsync(
        int serviceRequestId,
        string providerUserId,
        SubmitOfferRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Price <= 0)
        {
            return ServiceResult<OfferSnapshotDto>.Validation(
                "price",
                "Price must be greater than zero.");
        }

        if (request.EstimatedDate <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<OfferSnapshotDto>.Validation(
                "estimatedDate",
                "Estimated date must be in the future.");
        }

        var profile = await _requests.FindProviderProfileAsync(
            providerUserId,
            cancellationToken);
        if (profile is null)
        {
            return ServiceResult<OfferSnapshotDto>.NotFound("Service request not found.");
        }

        if (!profile.CanReceiveWork)
        {
            await _audit.RecordAsync(new AuditEntry
            {
                Category = AuditCategories.Offer,
                Action = AuditActions.OfferSubmitted,
                Outcome = AuditOutcomes.Denied,
                EntityType = nameof(ServiceRequest),
                EntityId = serviceRequestId.ToString(),
                Message = profile.IsSuspended
                    ? "Suspended provider attempted to submit an offer."
                    : "Unverified provider attempted to submit an offer.",
                Details = new Dictionary<string, object?>
                {
                    ["verificationStatus"] = profile.VerificationStatus.ToString(),
                    ["isSuspended"] = profile.IsSuspended
                }
            }, cancellationToken);

            return ServiceResult<OfferSnapshotDto>.Forbidden(
                profile.IsSuspended
                    ? "Your provider account is currently suspended."
                    : "Your professional verification must be approved before you can submit offers.");
        }

        var eligible = await _requests
            .EligibleOpenRequestsForProvider(profile)
            .AnyAsync(r => r.Id == serviceRequestId, cancellationToken);

        if (!eligible)
        {
            return ServiceResult<OfferSnapshotDto>.NotFound("Service request not found.");
        }

        var duplicate = await _db.Offers.AnyAsync(
            o => o.ServiceRequestId == serviceRequestId &&
                 o.ProviderId == providerUserId &&
                 o.Status != OfferStatus.Withdrawn,
            cancellationToken);

        if (duplicate)
        {
            return ServiceResult<OfferSnapshotDto>.Conflict(
                "You already have an active offer on this request.");
        }

        var offer = new Offer
        {
            ServiceRequestId = serviceRequestId,
            ProviderId = providerUserId,
            Price = request.Price,
            Message = request.Message.Trim(),
            EstimatedDate = request.EstimatedDate,
            Status = OfferStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Offers.Add(offer);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueConstraintViolation(ex))
        {
            return ServiceResult<OfferSnapshotDto>.Conflict(
                "You already have an active offer on this request.");
        }

        _logger.LogInformation(
            "Provider {UserId} submitted offer {OfferId} for request {ServiceRequestId}",
            providerUserId,
            offer.Id,
            serviceRequestId);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Offer,
            Action = AuditActions.OfferSubmitted,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Offer),
            EntityId = offer.Id.ToString(),
            Message = "Provider submitted an offer.",
            Details = new Dictionary<string, object?>
            {
                ["serviceRequestId"] = serviceRequestId,
                ["price"] = offer.Price
            }
        }, cancellationToken);

        return ServiceResult<OfferSnapshotDto>.Success(ToSnapshot(offer));
    }

    public async Task<ServiceResult<OfferSnapshotDto>> WithdrawAsync(
        int offerId,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(
            o => o.Id == offerId,
            cancellationToken);

        if (offer is null)
        {
            return ServiceResult<OfferSnapshotDto>.NotFound("Offer not found.");
        }

        if (offer.ProviderId != providerUserId)
        {
            return ServiceResult<OfferSnapshotDto>.Forbidden(
                "You do not own this offer.");
        }

        if (offer.Status != OfferStatus.Pending)
        {
            return ServiceResult<OfferSnapshotDto>.Conflict(
                "Only pending offers can be withdrawn.");
        }

        offer.Status = OfferStatus.Withdrawn;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provider {UserId} withdrew offer {OfferId}",
            providerUserId,
            offer.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Offer,
            Action = AuditActions.OfferWithdrawn,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Offer),
            EntityId = offer.Id.ToString(),
            Message = "Provider withdrew an offer."
        }, cancellationToken);

        return ServiceResult<OfferSnapshotDto>.Success(ToSnapshot(offer));
    }

    public async Task<ServiceResult<PagedResult<OfferMineDto>>> GetMineAsync(
        string providerUserId,
        PageQuery paging,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = paging.Normalize();
        var query = _db.Offers
            .AsNoTracking()
            .Where(o => o.ProviderId == providerUserId);

        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            if (!OfferStatusParser.TryParse(paging.Status, out var status))
            {
                return ServiceResult<PagedResult<OfferMineDto>>.Validation(
                    "status",
                    "Status is not valid.");
            }

            query = query.Where(o => o.Status == status);
        }

        var pageResult = await query
            .OrderByDescending(o => o.Id)
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
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<OfferMineDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<IReadOnlyList<OfferForCustomerDto>>> ListForRequestAsync(
        int serviceRequestId,
        string customerId,
        CancellationToken cancellationToken)
    {
        var request = await _db.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == serviceRequestId, cancellationToken);

        if (request is null)
        {
            return ServiceResult<IReadOnlyList<OfferForCustomerDto>>.NotFound(
                "Service request not found.");
        }

        if (request.CustomerId != customerId)
        {
            return ServiceResult<IReadOnlyList<OfferForCustomerDto>>.Forbidden(
                "You do not own this service request.");
        }

        var offers = await OfferProjections
            .ForCustomer(
                _db.Offers.AsNoTracking().Where(o => o.ServiceRequestId == serviceRequestId),
                request.Status == ServiceRequestStatus.Open)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<OfferForCustomerDto>>.Success(offers);
    }

    public async Task<ServiceResult<BookingDetailDto>> AcceptAsync(
        int offerId,
        string customerId,
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
                var offer = await _db.Offers
                    .Include(o => o.ServiceRequest)
                    .FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);

                if (offer is null)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.NotFound("Offer not found."),
                        cancellationToken);
                }

                if (offer.ServiceRequest.CustomerId != customerId)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.Forbidden(
                            "You do not own this service request."),
                        cancellationToken);
                }

                if (offer.Status != OfferStatus.Pending ||
                    offer.ServiceRequest.Status != ServiceRequestStatus.Open)
                {
                    var alreadyTaken =
                        offer.Status == OfferStatus.Accepted ||
                        offer.ServiceRequest.Status == ServiceRequestStatus.Booked;

                    return await Abort(
                        transaction,
                        ServiceResult<BookingDetailDto>.Conflict(
                            alreadyTaken
                                ? AnotherOfferAcceptedFirst
                                : "This offer cannot be accepted in its current state."),
                        cancellationToken);
                }

                offer.Status = OfferStatus.Accepted;
                offer.ServiceRequest.Status = ServiceRequestStatus.Booked;

                var siblings = await _db.Offers
                    .Where(o =>
                        o.ServiceRequestId == offer.ServiceRequestId &&
                        o.Id != offer.Id &&
                        o.Status == OfferStatus.Pending)
                    .ToListAsync(cancellationToken);

                foreach (var sibling in siblings)
                {
                    sibling.Status = OfferStatus.Rejected;
                }

                var booking = new Booking
                {
                    OfferId = offer.Id,
                    ServiceRequestId = offer.ServiceRequestId,
                    CustomerId = offer.ServiceRequest.CustomerId,
                    ProviderId = offer.ProviderId,
                    ScheduledDate = offer.EstimatedDate,
                    FinalPrice = offer.Price,
                    Status = BookingStatus.Scheduled,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                if (SqliteProvider.IsSqlite(_db))
                {
                    booking.RowVersion = [0];
                }

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Customer {UserId} accepted offer {OfferId} for request {ServiceRequestId}; booking {BookingId} created",
                    customerId,
                    offer.Id,
                    offer.ServiceRequestId,
                    booking.Id);

                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Offer,
                    Action = AuditActions.OfferAccepted,
                    Outcome = AuditOutcomes.Success,
                    EntityType = nameof(Offer),
                    EntityId = offer.Id.ToString(),
                    Message = "Customer accepted an offer.",
                    Details = new Dictionary<string, object?>
                    {
                        ["serviceRequestId"] = offer.ServiceRequestId,
                        ["bookingId"] = booking.Id
                    }
                }, cancellationToken);

                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Booking,
                    Action = AuditActions.BookingCreated,
                    Outcome = AuditOutcomes.Success,
                    EntityType = nameof(Booking),
                    EntityId = booking.Id.ToString(),
                    Message = "Booking created from accepted offer.",
                    Details = new Dictionary<string, object?>
                    {
                        ["offerId"] = offer.Id,
                        ["serviceRequestId"] = offer.ServiceRequestId
                    }
                }, cancellationToken);

                return await _bookings.GetByIdAsync(
                    booking.Id,
                    customerId,
                    isAdmin: false,
                    cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return ServiceResult<BookingDetailDto>.Conflict(AnotherOfferAcceptedFirst);
            }
            catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueConstraintViolation(ex))
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return ServiceResult<BookingDetailDto>.Conflict(AnotherOfferAcceptedFirst);
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

    private static OfferSnapshotDto ToSnapshot(Offer offer) => new()
    {
        Id = offer.Id,
        Price = offer.Price,
        Message = offer.Message,
        EstimatedDate = offer.EstimatedDate,
        Status = offer.Status.ToString(),
        CreatedAt = offer.CreatedAt
    };

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
