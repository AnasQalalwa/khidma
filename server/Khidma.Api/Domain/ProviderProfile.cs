using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class ProviderProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    public string City { get; set; } = default!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int YearsOfExperience { get; set; }

    public string? Bio { get; set; }

    public string? PhotoStoredFileName { get; set; }

    public string? PhotoContentType { get; set; }

    public ProviderVerificationStatus VerificationStatus { get; set; }

    public DateTimeOffset? VerificationReviewedAt { get; set; }

    public string? VerificationReviewedByUserId { get; set; }

    public string? VerificationRejectionReason { get; set; }

    public bool IsSuspended { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTimeOffset? SuspendedAt { get; set; }

    public string? SuspendedByUserId { get; set; }

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public ApplicationUser User { get; set; } = default!;

    public ICollection<ProviderService> ProviderServices { get; set; }
        = new List<ProviderService>();

    public ICollection<ProviderVerificationDocument> Documents { get; set; }
        = new List<ProviderVerificationDocument>();

    public ICollection<ProviderProfileChangeRequest> ChangeRequests { get; set; }
        = new List<ProviderProfileChangeRequest>();

    public ICollection<ProviderWorkingHour> WorkingHours { get; set; }
        = new List<ProviderWorkingHour>();

    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoStoredFileName);

    public bool CanReceiveWork =>
        VerificationStatus == ProviderVerificationStatus.Approved && !IsSuspended;

    public bool IsLocationLocked =>
        VerificationStatus == ProviderVerificationStatus.Approved;
}
