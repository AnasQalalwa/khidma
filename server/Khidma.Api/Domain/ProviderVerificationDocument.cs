using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class ProviderVerificationDocument
{
    public int Id { get; set; }

    public int ProviderProfileId { get; set; }

    public VerificationDocumentType DocumentType { get; set; }

    public string OriginalFileName { get; set; } = default!;

    public string StoredFileName { get; set; } = default!;

    public string ContentType { get; set; } = default!;

    public long FileSizeBytes { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public VerificationDocumentStatus ReviewStatus { get; set; }

    public string? ReviewNote { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public ProviderProfile ProviderProfile { get; set; } = default!;
}
