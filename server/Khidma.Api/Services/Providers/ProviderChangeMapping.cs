using Khidma.Api.Contracts.Providers;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Services.Providers;

internal static class ProviderChangeMapping
{
    public static ProviderChangeRequestDto ToDto(ProviderProfileChangeRequest change) => new()
    {
        Id = change.Id,
        Type = change.Type.ToString(),
        Status = change.Status.ToString(),
        RequestedCity = change.RequestedCity,
        RequestedLatitude = change.RequestedLatitude,
        RequestedLongitude = change.RequestedLongitude,
        ServiceId = change.ServiceId,
        ServiceName = change.Service?.Name,
        ProofDocumentId = change.ProofDocumentId,
        ProofFileName = change.ProofDocument?.OriginalFileName,
        ProofReviewStatus = change.ProofDocument?.ReviewStatus.ToString(),
        CreatedAt = change.CreatedAt,
        ReviewNote = change.ReviewNote
    };

    public static IReadOnlyList<ProviderChangeRequestDto> PendingOf(ProviderProfile profile) =>
        profile.ChangeRequests
            .Where(change => change.Status == ProviderChangeRequestStatus.Pending)
            .OrderByDescending(change => change.Id)
            .Select(ToDto)
            .ToList();

    public static VerificationDocumentDto ToDocumentDto(ProviderVerificationDocument document) =>
        new()
        {
            Id = document.Id,
            DocumentType = document.DocumentType.ToString(),
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAt = document.UploadedAt,
            ReviewStatus = document.ReviewStatus.ToString(),
            ReviewNote = document.ReviewNote,
            ReviewedAt = document.ReviewedAt,
            ServiceId = document.ServiceId,
            ServiceName = document.Service?.Name
        };
}
