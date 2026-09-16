using Khidma.Api.Contracts.Dashboard;

namespace Khidma.Api.Services.Dashboard;

public interface IDashboardService
{
    Task<ServiceResult<CustomerDashboardDto>> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderDashboardDto>> GetProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken);
}
