using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Services.Bookings;

public interface IBookingService
{
    Task<ServiceResult<PagedResult<BookingSummaryDto>>> GetMineAsync(
        string userId,
        string role,
        PageQuery paging,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingDetailDto>> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingDetailDto>> StartAsync(
        int id,
        string providerUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingDetailDto>> CompleteAsync(
        int id,
        string providerUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingDetailDto>> CancelAsync(
        int id,
        string userId,
        string reason,
        CancellationToken cancellationToken);
}

internal static class BookingStatusParser
{
    public static bool TryParse(string? value, out BookingStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status);
}
