using Khidma.Api.Contracts.Reviews;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khidma.Api.Services.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(AppDbContext db, ILogger<ReviewService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<ReviewDto>> CreateAsync(
        int bookingId,
        string customerId,
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5)
        {
            return ServiceResult<ReviewDto>.Validation(
                "rating",
                "Rating must be between 1 and 5.");
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
                var booking = await _db.Bookings
                    .Include(b => b.Review)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

                if (booking is null)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<ReviewDto>.NotFound("Booking not found."),
                        cancellationToken);
                }

                if (booking.CustomerId != customerId)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<ReviewDto>.Forbidden(
                            "Only the customer who booked this job can leave a review."),
                        cancellationToken);
                }

                if (booking.Status != BookingStatus.Completed)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<ReviewDto>.Conflict(
                            "A booking can only be reviewed after it is completed."),
                        cancellationToken);
                }

                if (booking.Review is not null)
                {
                    return await Abort(
                        transaction,
                        ServiceResult<ReviewDto>.Conflict(
                            "This booking already has a review."),
                        cancellationToken);
                }

                var review = new Review
                {
                    BookingId = booking.Id,
                    CustomerId = booking.CustomerId,
                    ProviderId = booking.ProviderId,
                    Rating = request.Rating,
                    Comment = string.IsNullOrWhiteSpace(request.Comment)
                        ? null
                        : request.Comment.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.Reviews.Add(review);
                await _db.SaveChangesAsync(cancellationToken);

                var profile = await _db.ProviderProfiles.FirstOrDefaultAsync(
                    p => p.UserId == booking.ProviderId,
                    cancellationToken);

                if (profile is not null)
                {
                    var stats = await _db.Reviews
                        .Where(r => r.ProviderId == booking.ProviderId)
                        .GroupBy(_ => 1)
                        .Select(g => new
                        {
                            Count = g.Count(),
                            Average = g.Average(r => r.Rating)
                        })
                        .FirstAsync(cancellationToken);

                    profile.ReviewCount = stats.Count;
                    profile.AverageRating = Math.Round(
                        (decimal)stats.Average,
                        2,
                        MidpointRounding.AwayFromZero);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Customer {UserId} reviewed booking {BookingId} for provider {ProviderId}",
                    customerId,
                    booking.Id,
                    booking.ProviderId);

                return ServiceResult<ReviewDto>.Success(new ReviewDto
                {
                    Id = review.Id,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    CustomerDisplayName = booking.Customer.FullName
                });
            }
            catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueConstraintViolation(ex))
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return ServiceResult<ReviewDto>.Conflict(
                    "This booking already has a review.");
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

    public async Task<ServiceResult<ReviewDto>> GetForBookingAsync(
        int bookingId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Review)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            return ServiceResult<ReviewDto>.NotFound("Booking not found.");
        }

        if (!isAdmin && booking.CustomerId != userId && booking.ProviderId != userId)
        {
            return ServiceResult<ReviewDto>.Forbidden(
                "You are not a participant in this booking.");
        }

        if (booking.Review is null)
        {
            return ServiceResult<ReviewDto>.NotFound("Review not found.");
        }

        return ServiceResult<ReviewDto>.Success(new ReviewDto
        {
            Id = booking.Review.Id,
            Rating = booking.Review.Rating,
            Comment = booking.Review.Comment,
            CreatedAt = booking.Review.CreatedAt,
            CustomerDisplayName = booking.Customer.FullName
        });
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
