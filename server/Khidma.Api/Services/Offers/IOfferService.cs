using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Services.Offers;

public interface IOfferService
{
    Task<ServiceResult<OfferSnapshotDto>> SubmitAsync(
        int serviceRequestId,
        string providerUserId,
        SubmitOfferRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<OfferSnapshotDto>> WithdrawAsync(
        int offerId,
        string providerUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<OfferMineDto>>> GetMineAsync(
        string providerUserId,
        PageQuery paging,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<OfferForCustomerDto>>> ListForRequestAsync(
        int serviceRequestId,
        string customerId,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingDetailDto>> AcceptAsync(
        int offerId,
        string customerId,
        CancellationToken cancellationToken);
}

internal static class OfferStatusParser
{
    public static bool TryParse(string? value, out OfferStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status);
}
