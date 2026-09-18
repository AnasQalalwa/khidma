using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class ProviderProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    public string City { get; set; } = default!;

    public int YearsOfExperience { get; set; }

    public string? Bio { get; set; }

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

    public bool CanReceiveWork =>
        VerificationStatus == ProviderVerificationStatus.Approved && !IsSuspended;
}
