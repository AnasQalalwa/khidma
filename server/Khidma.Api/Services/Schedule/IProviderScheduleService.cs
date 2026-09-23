using Khidma.Api.Contracts.Schedule;

namespace Khidma.Api.Services.Schedule;

public interface IProviderScheduleService
{
    Task<ServiceResult<WorkingHoursDto>> GetWorkingHoursAsync(
        string providerUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<WorkingHoursDto>> ReplaceWorkingHoursAsync(
        string providerUserId,
        UpdateWorkingHoursRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderScheduleDto>> GetMyScheduleAsync(
        string providerUserId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProviderAvailabilityDto>> GetAvailabilityAsync(
        int providerProfileId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);
}
