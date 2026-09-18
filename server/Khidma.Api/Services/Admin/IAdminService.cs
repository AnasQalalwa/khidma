using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Audit;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Common;

namespace Khidma.Api.Services.Admin;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken);

    Task<ServiceResult<AdminOverviewDto>> GetOverviewAsync(
        string? range,
        CancellationToken cancellationToken);

    Task<AdminAttentionDto> GetAttentionAsync(CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AdminProviderListItemDto>>> GetProvidersAsync(
        AdminProviderQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AdminUserListItemDto>>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminUserDetailDto>> GetUserAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AuditLogListItemDto>>> GetAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<AuditLogDetailDto>> GetAuditLogAsync(
        long id,
        CancellationToken cancellationToken);

    Task<AuditSummaryDto> GetAuditSummaryAsync(CancellationToken cancellationToken);

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
