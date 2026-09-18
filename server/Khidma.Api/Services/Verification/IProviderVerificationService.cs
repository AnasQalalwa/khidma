using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Verification;

namespace Khidma.Api.Services.Verification;

public interface IProviderVerificationService
{
    Task<ServiceResult<ProviderVerificationDto>> GetMineAsync(
        string providerUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderVerificationDto>> UploadAsync(
        string providerUserId,
        string documentType,
        IFormFile file,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderVerificationDto>> DeleteMineAsync(
        string providerUserId,
        int documentId,
        CancellationToken cancellationToken);

    Task<ServiceResult<DocumentDownloadResult>> DownloadAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AdminVerificationListItemDto>>> ListVerificationsAsync(
        AdminVerificationQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderVerificationDto>> GetAdminDetailAsync(
        int providerProfileId,
        CancellationToken cancellationToken);

    Task<ServiceResult<VerificationDocumentDto>> ReviewDocumentAsync(
        int documentId,
        ReviewVerificationDocumentRequest request,
        string adminUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderVerificationDto>> DecideProviderAsync(
        int providerProfileId,
        ProviderVerificationDecisionRequest request,
        string adminUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderVerificationDto>> SetSuspensionAsync(
        int providerProfileId,
        SetProviderSuspensionRequest request,
        string adminUserId,
        CancellationToken cancellationToken);
}
