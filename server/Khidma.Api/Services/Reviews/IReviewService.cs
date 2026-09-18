using Khidma.Api.Contracts.Reviews;

namespace Khidma.Api.Services.Reviews;

public interface IReviewService
{
    Task<ServiceResult<ReviewDto>> CreateAsync(
        int bookingId,
        string customerId,
        CreateReviewRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ReviewDto>> GetForBookingAsync(
        int bookingId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken);
}
