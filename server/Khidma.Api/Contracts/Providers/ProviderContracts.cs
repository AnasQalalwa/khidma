using System.ComponentModel.DataAnnotations;
using Khidma.Api.Auth;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Reviews;

using Khidma.Api.Contracts.Schedule;

namespace Khidma.Api.Contracts.Providers;

public sealed class UpdateProviderProfileRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = default!;

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [Range(0, 80)]
    public int YearsOfExperience { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Required(ErrorMessage = PhoneRules.RequirementMessage)]
    [PhoneNumber]
    public string PhoneNumber { get; set; } = default!;
}

public sealed class ReplaceProviderServicesRequest
{
    [Required]
    public IReadOnlyList<int> ServiceIds { get; set; } = [];
}

public sealed class RequestLocationChangeRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = default!;

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }
}

public sealed class ReviewProviderChangeRequest
{
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = default!;

    [StringLength(1000)]
    public string? Note { get; set; }
}

public sealed class ProviderChangeRequestDto
{
    public required int Id { get; init; }

    public required string Type { get; init; }

    public required string Status { get; init; }

    public string? RequestedCity { get; init; }

    public decimal? RequestedLatitude { get; init; }

    public decimal? RequestedLongitude { get; init; }

    public int? ServiceId { get; init; }

    public string? ServiceName { get; init; }

    public int? ProofDocumentId { get; init; }

    public string? ProofFileName { get; init; }

    public string? ProofReviewStatus { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string? ReviewNote { get; init; }
}

public sealed class ProviderMeDto
{
    public required int Id { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string PhoneNumber { get; init; }

    public required string City { get; init; }

    public decimal? Latitude { get; init; }

    public decimal? Longitude { get; init; }

    public required int YearsOfExperience { get; init; }

    public string? Bio { get; init; }

    public required string VerificationStatus { get; init; }

    public required bool IsSuspended { get; init; }

    public string? SuspensionReason { get; init; }

    public string? VerificationRejectionReason { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<ServiceDto> Services { get; init; }

    public required bool HasPhoto { get; init; }

    public required bool CanEditLocation { get; init; }

    public required bool CanEditServices { get; init; }

    public required IReadOnlyList<ProviderChangeRequestDto> PendingChanges { get; init; }
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

    public required bool HasPhoto { get; init; }

    public required IReadOnlyList<WorkingHourDto> WorkingHours { get; init; }
}
