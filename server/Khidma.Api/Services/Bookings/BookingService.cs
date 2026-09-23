using Khidma.Api.Auth;
using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Reviews;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Infrastructure;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

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

    public async Task<ServiceResult<BookingDetailDto>> CreateAsync(
        string customerId,
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RequestedDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "requestedDate",
                "Choose today or a later day.");
        }

        var customer = await _db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == customerId, cancellationToken);
        if (customer is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Customer profile not found.");
        }

        var serviceExists = await _db.Services
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.ServiceId, cancellationToken);
        if (!serviceExists)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Service not found.");
        }

        var provider = await _db.ProviderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProviderProfileId, cancellationToken);
        if (provider is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Provider not found.");
        }

        if (!provider.CanReceiveWork)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "This provider is not available for bookings.");
        }

        var offersService = await _db.ProviderServices
            .AsNoTracking()
            .AnyAsync(
                ps => ps.ProviderProfileId == provider.Id && ps.ServiceId == request.ServiceId,
                cancellationToken);
        if (!offersService)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "This provider does not offer that service.");
        }

        var weekday = (int)request.RequestedDate.DayOfWeek;
        var worksThatDay = await _db.ProviderWorkingHours
            .AsNoTracking()
            .AnyAsync(
                h => h.ProviderProfileId == provider.Id && h.DayOfWeek == weekday,
                cancellationToken);
        if (!worksThatDay)
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "requestedDate",
                "This provider does not work on that day.");
        }

        var duplicate = await _db.Bookings.AnyAsync(
            b => b.CustomerId == customerId &&
                 b.ProviderId == provider.UserId &&
                 b.ServiceId == request.ServiceId &&
                 b.Status == BookingStatus.Pending,
            cancellationToken);
        if (duplicate)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "You already have a pending booking with this provider for this service.");
        }

        var booking = new Booking
        {
            CustomerId = customerId,
            ProviderId = provider.UserId,
            ServiceId = request.ServiceId,
            City = customer.City,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RequestedDate = request.RequestedDate,
            Status = BookingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Bookings.Add(booking);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueConstraintViolation(ex))
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "You already have a pending booking with this provider for this service.");
        }

        _logger.LogInformation(
            "Customer {UserId} requested booking {BookingId} with provider {ProviderId}",
            customerId,
            booking.Id,
            provider.UserId);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingRequested,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Customer requested a booking.",
            Details = new Dictionary<string, object?>
            {
                ["providerId"] = provider.UserId,
                ["serviceId"] = request.ServiceId
            }
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, customerId, isAdmin: false, cancellationToken);
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
            var statuses = new List<BookingStatus>();
            foreach (var part in paging.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!BookingStatusParser.TryParse(part, out var status))
                {
                    return ServiceResult<PagedResult<BookingSummaryDto>>.Validation(
                        "status",
                        "Status is not valid.");
                }

                statuses.Add(status);
            }

            query = query.Where(b => statuses.Contains(b.Status));
        }

        var pageResult = await query
            .OrderByDescending(b => b.Id)
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
                CounterpartyName = isProvider
                    ? b.Customer.FullName
                    : b.Provider.FullName,
                CreatedAt = b.CreatedAt,
                HasReview = b.Review != null
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        foreach (var item in pageResult.Items)
        {
            if (item.ScheduledStart is not null && item.DurationHours is not null)
            {
                item.ScheduledEnd = item.ScheduledStart.Value.AddHours(item.DurationHours.Value);
            }
        }

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

    public async Task<ServiceResult<BookingDetailDto>> AcceptAsync(
        int id,
        string providerUserId,
        AcceptBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Price <= 0)
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "price",
                "Enter a price greater than zero.");
        }

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        if (booking.ProviderId != providerUserId)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "Only the assigned provider can accept this booking.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "Only pending bookings can be accepted.");
        }

        var slotError = await ValidateSlotAsync(
            providerUserId,
            id,
            request.ScheduledStart,
            request.DurationHours,
            requireFutureStart: true,
            cancellationToken);
        if (slotError is not null)
        {
            return slotError;
        }

        booking.Status = BookingStatus.Scheduled;
        booking.QuotedPrice = request.Price;
        booking.ProviderMessage = string.IsNullOrWhiteSpace(request.Message)
            ? null
            : request.Message.Trim();
        booking.ScheduledStart = request.ScheduledStart;
        booking.DurationHours = request.DurationHours;
        booking.RespondedAt = DateTimeOffset.UtcNow;
        var conflict = await SaveOrConflictAsync(cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _logger.LogInformation(
            "Provider {UserId} accepted booking {BookingId}",
            providerUserId,
            booking.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingAccepted,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Provider accepted a booking.",
            Details = new Dictionary<string, object?>
            {
                ["price"] = request.Price,
                ["scheduledStart"] = request.ScheduledStart,
                ["durationHours"] = request.DurationHours
            }
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, providerUserId, isAdmin: false, cancellationToken);
    }

    public async Task<ServiceResult<BookingDetailDto>> DeclineAsync(
        int id,
        string providerUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "reason",
                "A reason is required.");
        }

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        if (booking.ProviderId != providerUserId)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "Only the assigned provider can decline this booking.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "Only pending bookings can be declined.");
        }

        booking.Status = BookingStatus.Declined;
        booking.DeclineReason = reason.Trim();
        booking.RespondedAt = DateTimeOffset.UtcNow;
        var conflict = await SaveOrConflictAsync(cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _logger.LogInformation(
            "Provider {UserId} declined booking {BookingId}",
            providerUserId,
            booking.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingDeclined,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Provider declined a booking."
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, providerUserId, isAdmin: false, cancellationToken);
    }

    public async Task<ServiceResult<BookingDetailDto>> StartAsync(
        int id,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
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
        var conflict = await SaveOrConflictAsync(cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

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
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        if (booking.ProviderId != providerUserId)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "Only the assigned provider can complete this booking.");
        }

        if (booking.Status != BookingStatus.InProgress)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "Only in-progress bookings can be completed.");
        }

        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = DateTimeOffset.UtcNow;
        var conflict = await SaveOrConflictAsync(cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _logger.LogInformation(
            "Provider {UserId} completed booking {BookingId}",
            providerUserId,
            booking.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingCompleted,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Provider completed a booking."
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, providerUserId, isAdmin: false, cancellationToken);
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

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        var isCustomer = booking.CustomerId == userId;
        var isProvider = booking.ProviderId == userId;
        if (!isCustomer && !isProvider)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "You are not a participant in this booking.");
        }

        var allowed = isCustomer
            ? booking.Status is BookingStatus.Pending or BookingStatus.Scheduled
            : booking.Status == BookingStatus.Scheduled;
        if (!allowed)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                isProvider && booking.Status == BookingStatus.Pending
                    ? "Decline a pending booking instead of cancelling it."
                    : "This booking can no longer be cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTimeOffset.UtcNow;
        booking.CancellationReason = reason.Trim();
        var conflict = await SaveOrConflictAsync(cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _logger.LogInformation(
            "User {UserId} cancelled booking {BookingId}",
            userId,
            booking.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingCancelled,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Booking cancelled."
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, userId, isAdmin: false, cancellationToken);
    }

    public async Task<ServiceResult<BookingDetailDto>> RescheduleAsync(
        int id,
        string providerUserId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null)
        {
            return ServiceResult<BookingDetailDto>.NotFound("Booking not found.");
        }

        if (booking.ProviderId != providerUserId)
        {
            return ServiceResult<BookingDetailDto>.Forbidden(
                "Only the assigned provider can reschedule this booking.");
        }

        if (booking.Status is not (BookingStatus.Scheduled or BookingStatus.InProgress))
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "Only scheduled or in-progress bookings can be rescheduled.");
        }

        var slotError = await ValidateSlotAsync(
            providerUserId,
            id,
            request.ScheduledStart,
            request.DurationHours,
            requireFutureStart: false,
            cancellationToken);
        if (slotError is not null)
        {
            return slotError;
        }

        booking.ScheduledStart = request.ScheduledStart;
        booking.DurationHours = request.DurationHours;
        booking.RescheduledAt = DateTimeOffset.UtcNow;
        booking.RescheduleNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var conflict = await SaveOrConflictAsync(cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _logger.LogInformation(
            "Provider {UserId} rescheduled booking {BookingId}",
            providerUserId,
            booking.Id);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Booking,
            Action = AuditActions.BookingRescheduled,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(Booking),
            EntityId = booking.Id.ToString(),
            Message = "Provider rescheduled a booking.",
            Details = new Dictionary<string, object?>
            {
                ["scheduledStart"] = request.ScheduledStart,
                ["durationHours"] = request.DurationHours
            }
        }, cancellationToken);

        return await GetByIdAsync(booking.Id, providerUserId, isAdmin: false, cancellationToken);
    }

    private async Task<ServiceResult<BookingDetailDto>?> ValidateSlotAsync(
        string providerUserId,
        int bookingId,
        DateTimeOffset start,
        int durationHours,
        bool requireFutureStart,
        CancellationToken cancellationToken)
    {
        if (durationHours is < 1 or > 12)
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "durationHours",
                "Duration must be between 1 and 12 hours.");
        }

        if (requireFutureStart && start <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "scheduledStart",
                "Choose a start time in the future.");
        }

        var end = start.AddHours(durationHours);
        if (end <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<BookingDetailDto>.Validation(
                "scheduledStart",
                "Choose a time that has not already ended.");
        }

        var others = await _db.Bookings
            .AsNoTracking()
            .Where(b =>
                b.ProviderId == providerUserId &&
                b.Id != bookingId &&
                (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InProgress) &&
                b.ScheduledStart != null &&
                b.DurationHours != null)
            .Select(b => new
            {
                b.ScheduledStart,
                b.DurationHours,
                ServiceName = b.Service.Name
            })
            .ToListAsync(cancellationToken);

        var clash = others.FirstOrDefault(other =>
        {
            var otherStart = other.ScheduledStart!.Value;
            var otherEnd = otherStart.AddHours(other.DurationHours!.Value);
            return start < otherEnd && end > otherStart;
        });
        if (clash is not null)
        {
            var otherStart = clash.ScheduledStart!.Value;
            var otherEnd = otherStart.AddHours(clash.DurationHours!.Value);
            return ServiceResult<BookingDetailDto>.Conflict(
                $"That time overlaps {clash.ServiceName} ({otherStart:g} – {otherEnd:g}).");
        }

        return null;
    }

    private async Task<ServiceResult<BookingDetailDto>?> SaveOrConflictAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<BookingDetailDto>.Conflict(
                "This booking was updated by someone else.");
        }
    }

    private IQueryable<Booking> DetailQuery() =>
        _db.Bookings
            .AsNoTracking()
            .Include(b => b.Service)
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
            ServiceId = booking.ServiceId,
            ProviderProfileId = booking.Provider.ProviderProfile?.Id ?? 0,
            ServiceName = booking.Service.Name,
            CategoryName = booking.Service.Category.Name,
            City = booking.City,
            Notes = booking.Notes,
            RequestedDate = booking.RequestedDate,
            ScheduledStart = booking.ScheduledStart,
            ScheduledEnd = booking.ScheduledStart is null || booking.DurationHours is null
                ? null
                : booking.ScheduledStart.Value.AddHours(booking.DurationHours.Value),
            DurationHours = booking.DurationHours,
            RescheduledAt = booking.RescheduledAt,
            RescheduleNote = booking.RescheduleNote,
            QuotedPrice = booking.QuotedPrice,
            ProviderMessage = booking.ProviderMessage,
            DeclineReason = booking.DeclineReason,
            Status = booking.Status.ToString(),
            CustomerId = booking.CustomerId,
            ProviderId = booking.ProviderId,
            CustomerName = booking.Customer.FullName,
            ProviderName = booking.Provider.FullName,
            CustomerPhone = booking.Customer.PhoneNumber,
            ProviderPhone = booking.Provider.PhoneNumber,
            CustomerCity = booking.Customer.CustomerProfile?.City,
            ProviderCity = booking.Provider.ProviderProfile?.City,
            CreatedAt = booking.CreatedAt,
            RespondedAt = booking.RespondedAt,
            StartedAt = booking.StartedAt,
            CompletedAt = booking.CompletedAt,
            CancelledAt = booking.CancelledAt,
            CancellationReason = booking.CancellationReason,
            CanAccept = isProvider && booking.Status == BookingStatus.Pending,
            CanReschedule = isProvider &&
                            booking.Status is BookingStatus.Scheduled or BookingStatus.InProgress,
            CanDecline = isProvider && booking.Status == BookingStatus.Pending,
            CanStart = isProvider && booking.Status == BookingStatus.Scheduled,
            CanComplete = isProvider && booking.Status == BookingStatus.InProgress,
            CanCancel = (isCustomer &&
                         booking.Status is BookingStatus.Pending or BookingStatus.Scheduled) ||
                        (isProvider && booking.Status == BookingStatus.Scheduled),
            CanReview = isCustomer &&
                        booking.Status == BookingStatus.Completed &&
                        booking.Review is null,
            Review = review
        };
    }
}
