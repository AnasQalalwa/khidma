using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class ProviderProfileChangeRequest
{
    public int Id { get; set; }

    public int ProviderProfileId { get; set; }

    public ProviderChangeRequestType Type { get; set; }

    public ProviderChangeRequestStatus Status { get; set; }

    public string? RequestedCity { get; set; }

    public decimal? RequestedLatitude { get; set; }

    public decimal? RequestedLongitude { get; set; }

    public int? ServiceId { get; set; }

    public int? ProofDocumentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? ReviewNote { get; set; }

    public ProviderProfile ProviderProfile { get; set; } = default!;

    public Service? Service { get; set; }

    public ProviderVerificationDocument? ProofDocument { get; set; }
}
