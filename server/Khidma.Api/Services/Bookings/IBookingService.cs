using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Services.Bookings;

public interface IBookingService
{
    Task<ServiceResult<BookingDetailDto>> CreateAsync(
        string customerId,
        CreateBookingRequest request,
        CancellationToken cancellationToken);

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

    Task<ServiceResult<BookingDetailDto>> AcceptAsync(
        int id,
        string providerUserId,
        AcceptBookingRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingDetailDto>> DeclineAsync(
        int id,
        string providerUserId,
        string reason,
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

    Task<ServiceResult<BookingDetailDto>> RescheduleAsync(
        int id,
        string providerUserId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken);
}

internal static class BookingStatusParser
{
    public static bool TryParse(string? value, out BookingStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status);
}
