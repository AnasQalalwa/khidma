using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Schedule;

public sealed class WorkingHourDto
{
    public int DayOfWeek { get; set; }

    public int Hour { get; set; }
}

public sealed class WorkingHoursDto
{
    public required IReadOnlyList<WorkingHourDto> Hours { get; init; }
}

public sealed class UpdateWorkingHoursRequest
{
    [Required]
    public List<WorkingHourDto> Hours { get; set; } = [];
}

public sealed class BusyIntervalDto
{
    public required DateTimeOffset Start { get; init; }

    public required DateTimeOffset End { get; init; }
}

public sealed class ProviderAvailabilityDto
{
    public required IReadOnlyList<WorkingHourDto> WorkingHours { get; init; }

    public required IReadOnlyList<BusyIntervalDto> Busy { get; init; }
}

public sealed class ScheduleEntryDto
{
    public required int BookingId { get; init; }

    public required string ServiceName { get; init; }

    public required string CustomerName { get; init; }

    public required string Status { get; init; }

    public DateTimeOffset? Start { get; init; }

    public DateTimeOffset? End { get; init; }

    public DateOnly? RequestedDate { get; init; }
}

public sealed class ProviderScheduleDto
{
    public required IReadOnlyList<WorkingHourDto> WorkingHours { get; init; }

    public required IReadOnlyList<ScheduleEntryDto> Items { get; init; }
}
