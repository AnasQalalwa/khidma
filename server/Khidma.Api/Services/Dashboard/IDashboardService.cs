using Khidma.Api.Contracts.Dashboard;

namespace Khidma.Api.Services.Dashboard;

public interface IDashboardService
{
    Task<ServiceResult<ProviderDashboardDto>> GetProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken);
}
