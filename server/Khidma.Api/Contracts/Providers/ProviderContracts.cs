using System.ComponentModel.DataAnnotations;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Reviews;

namespace Khidma.Api.Contracts.Providers;

public sealed class UpdateProviderProfileRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = default!;

    [Range(0, 80)]
    public int YearsOfExperience { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }
}

public sealed class ReplaceProviderServicesRequest
{
    [Required]
    public IReadOnlyList<int> ServiceIds { get; set; } = [];
}

public sealed class ProviderMeDto
{
    public required int Id { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string City { get; init; }

    public required int YearsOfExperience { get; init; }

    public string? Bio { get; init; }

    public required string VerificationStatus { get; init; }

    public required bool IsSuspended { get; init; }

    public string? SuspensionReason { get; init; }

    public string? VerificationRejectionReason { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<ServiceDto> Services { get; init; }
}

public sealed class PublicProviderDto
{
    public required int Id { get; init; }

    public required string FullName { get; init; }

    public required string City { get; init; }

    public required int YearsOfExperience { get; init; }

    public string? Bio { get; init; }

    public required bool IsVerified { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<ServiceDto> Services { get; init; }

    public required IReadOnlyList<PublicReviewDto> RecentReviews { get; init; }
}
