using Khidma.Api.Contracts.Providers;
using Khidma.Api.Contracts.Verification;

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

    Task<ServiceResult<ProviderMeDto>> RequestLocationChangeAsync(
        string userId,
        RequestLocationChangeRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderMeDto>> RequestServiceAdditionAsync(
        string userId,
        int serviceId,
        string documentType,
        IFormFile file,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderMeDto>> UploadPhotoAsync(
        string userId,
        IFormFile file,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderMeDto>> DeletePhotoAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<DocumentDownloadResult>> GetPhotoAsync(
        int providerProfileId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderChangeRequestDto>> ReviewChangeRequestAsync(
        int changeRequestId,
        ReviewProviderChangeRequest request,
        string adminUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicProviderDto>> GetPublicAsync(
        int providerProfileId,
        CancellationToken cancellationToken);
}
