using Khidma.Api.Contracts.Providers;

namespace Khidma.Api.Services.Providers;

public interface IProviderProfileService
{
    Task<ServiceResult<ProviderMeDto>> GetMeAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderMeDto>> UpdateMeAsync(
        string userId,
        UpdateProviderProfileRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderMeDto>> ReplaceServicesAsync(
        string userId,
        ReplaceProviderServicesRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicProviderDto>> GetPublicAsync(
        int providerProfileId,
        CancellationToken cancellationToken);
}
