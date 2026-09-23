using Khidma.Api.Contracts.Schedule;

namespace Khidma.Api.Contracts.Catalog;

public sealed class ServiceProviderDto
{
    public required int Id { get; init; }

    public required string FullName { get; init; }

    public required string City { get; init; }

    public required int YearsOfExperience { get; init; }

    public string? Bio { get; init; }

    public required bool IsVerified { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required bool HasPhoto { get; init; }

    public required int CompletedJobs { get; init; }

    public required IReadOnlyList<WorkingHourDto> WorkingHours { get; init; }
}
