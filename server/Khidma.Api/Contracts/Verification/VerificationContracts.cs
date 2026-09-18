using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Verification;

public sealed class ProviderVerificationDto
{
    public required int ProviderProfileId { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string City { get; init; }

    public required int YearsOfExperience { get; init; }

    public string? Bio { get; init; }

    public required string VerificationStatus { get; init; }

    public string? VerificationRejectionReason { get; init; }

    public DateTimeOffset? VerificationReviewedAt { get; init; }

    public required bool IsSuspended { get; init; }

    public string? SuspensionReason { get; init; }

    public DateTimeOffset? SuspendedAt { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<string> Services { get; init; }

    public required IReadOnlyList<VerificationDocumentDto> Documents { get; init; }

    public required bool HasApprovedDocument { get; init; }
}

public sealed class VerificationDocumentDto
{
    public required int Id { get; init; }

    public required string DocumentType { get; init; }

    public required string OriginalFileName { get; init; }

    public required string ContentType { get; init; }

    public required long FileSizeBytes { get; init; }

    public required DateTimeOffset UploadedAt { get; init; }

    public required string ReviewStatus { get; init; }

    public string? ReviewNote { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }
}

public sealed class AdminVerificationQuery : Common.PageQuery
{
    public string? VerificationStatus { get; set; }

    public string? DocumentStatus { get; set; }

    public string? Search { get; set; }
}

public sealed class AdminVerificationListItemDto
{
    public required int ProviderProfileId { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string City { get; init; }

    public required int YearsOfExperience { get; init; }

    public required string VerificationStatus { get; init; }

    public required bool IsSuspended { get; init; }

    public required int DocumentCount { get; init; }

    public required int PendingDocumentCount { get; init; }

    public required int ApprovedDocumentCount { get; init; }

    public required int RejectedDocumentCount { get; init; }

    public required IReadOnlyList<string> Services { get; init; }
}

public sealed class ReviewVerificationDocumentRequest
{
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = default!;

    [StringLength(1000)]
    public string? Note { get; set; }
}

public sealed class ProviderVerificationDecisionRequest
{
    [Required]
    [StringLength(32)]
    public string Status { get; set; } = default!;

    [StringLength(1000)]
    public string? Reason { get; set; }
}

public sealed class SetProviderSuspensionRequest
{
    public bool Suspended { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public sealed class DocumentDownloadResult
{
    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public required string OriginalFileName { get; init; }
}
