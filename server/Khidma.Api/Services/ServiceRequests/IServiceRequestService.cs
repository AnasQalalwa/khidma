using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Contracts.ServiceRequests;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Services.ServiceRequests;

public interface IServiceRequestService
{
    IQueryable<ServiceRequest> EligibleOpenRequestsForProvider(ProviderProfile provider);

    Task<ProviderProfile?> FindProviderProfileAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RequestDetailForCustomerDto>> CreateAsync(
        string customerId,
        CreateServiceRequestRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<ServiceRequestSummaryDto>>> GetMineAsync(
        string customerId,
        PageQuery paging,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<RequestSummaryForProviderDto>>> GetAvailableAsync(
        string providerUserId,
        PageQuery paging,
        CancellationToken cancellationToken);

    Task<ServiceResult<object>> GetByIdAsync(
        int id,
        string userId,
        string role,
        CancellationToken cancellationToken);

    Task<ServiceResult<RequestDetailForCustomerDto>> UpdateAsync(
        int id,
        string customerId,
        UpdateServiceRequestRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<RequestDetailForCustomerDto>> CancelAsync(
        int id,
        string customerId,
        CancellationToken cancellationToken);
}

internal static class RequestValidation
{
    public static ServiceResult<T>? ValidateScheduleAndBudget<T>(
        DateTimeOffset preferredDate,
        decimal? budgetMin,
        decimal? budgetMax)
    {
        if (preferredDate <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<T>.Validation(
                "preferredDate",
                "Preferred date must be in the future.");
        }

        if (budgetMin is < 0)
        {
            return ServiceResult<T>.Validation(
                "budgetMin",
                "Minimum budget cannot be negative.");
        }

        if (budgetMax is < 0)
        {
            return ServiceResult<T>.Validation(
                "budgetMax",
                "Maximum budget cannot be negative.");
        }

        if (budgetMin is not null && budgetMax is not null && budgetMax < budgetMin)
        {
            return ServiceResult<T>.Validation(
                "budgetMax",
                "Maximum budget must be greater than or equal to the minimum budget.");
        }

        return null;
    }

    public static bool TryParseStatus(string? value, out ServiceRequestStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status);

    public static string FirstName(string fullName)
    {
        var trimmed = fullName.Trim();
        var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? trimmed : trimmed[..space];
    }

    public static string FormatStatus<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        value.ToString();
}

internal static class OfferProjections
{
    public static IQueryable<OfferForCustomerDto> ForCustomer(
        IQueryable<Offer> offers,
        bool requestIsOpen)
    {
        return offers.Select(o => new OfferForCustomerDto
        {
            Id = o.Id,
            ProviderId = o.ProviderId,
            ProviderProfileId = o.Provider.ProviderProfile!.Id,
            ProviderDisplayName = o.Provider.FullName,
            ProviderAverageRating = o.Provider.ProviderProfile!.AverageRating,
            ProviderReviewCount = o.Provider.ProviderProfile!.ReviewCount,
            Price = o.Price,
            Message = o.Message,
            EstimatedDate = o.EstimatedDate,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt,
            CanAccept = requestIsOpen && o.Status == OfferStatus.Pending
        });
    }
}
