using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Common;

namespace Khidma.Api.Services.Admin;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AdminProviderListItemDto>>> GetProvidersAsync(
        PageQuery paging,
        bool? approved,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProviderListItemDto>> SetApprovalAsync(
        int providerProfileId,
        bool isApproved,
        string adminUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoryDto>> CreateCategoryAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(
        int id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteCategoryAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServiceDto>> CreateServiceAsync(
        SaveServiceRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServiceDto>> UpdateServiceAsync(
        int id,
        SaveServiceRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteServiceAsync(
        int id,
        CancellationToken cancellationToken);
}
