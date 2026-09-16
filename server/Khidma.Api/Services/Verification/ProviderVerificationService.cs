using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Infrastructure;
using Khidma.Api.Services.Audit;
using Khidma.Api.Services.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khidma.Api.Services.Verification;

public sealed class ProviderVerificationService : IProviderVerificationService
{
    private readonly AppDbContext _db;
    private readonly IProviderDocumentStorage _storage;
    private readonly IAuditService _audit;

    public ProviderVerificationService(
        AppDbContext db,
        IProviderDocumentStorage storage,
        IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<ServiceResult<ProviderVerificationDto>> GetMineAsync(
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var profile = await LoadProfileByUserAsync(providerUserId, cancellationToken);
        return profile is null
            ? ServiceResult<ProviderVerificationDto>.NotFound("Provider profile not found.")
            : ServiceResult<ProviderVerificationDto>.Success(ToDto(profile));
    }

    public async Task<ServiceResult<ProviderVerificationDto>> UploadAsync(
        string providerUserId,
        string documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<VerificationDocumentType>(documentType, ignoreCase: true, out var type) ||
            !Enum.IsDefined(type))
        {
            return ServiceResult<ProviderVerificationDto>.Validation(
                "documentType",
                "Document type is not valid.");
        }

        var validation = DocumentFileValidator.Validate(file);
        if (!validation.Succeeded)
        {
            return ServiceResult<ProviderVerificationDto>.Validation("file", validation.Error!);
        }

        var profile = await _db.ProviderProfiles
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.UserId == providerUserId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderVerificationDto>.NotFound("Provider profile not found.");
        }

        string storedFileName;
        await using (var buffer = new MemoryStream())
        {
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            storedFileName = await _storage.SaveAsync(
                buffer,
                validation.CanonicalExtension,
                cancellationToken);
        }

        var document = new ProviderVerificationDocument
        {
            ProviderProfileId = profile.Id,
            DocumentType = type,
            OriginalFileName = validation.SafeOriginalFileName,
            StoredFileName = storedFileName,
            ContentType = validation.CanonicalContentType,
            FileSizeBytes = validation.FileSizeBytes,
            UploadedAt = DateTimeOffset.UtcNow,
            ReviewStatus = VerificationDocumentStatus.Pending
        };

        _db.ProviderVerificationDocuments.Add(document);

        var resubmitted = profile.VerificationStatus == ProviderVerificationStatus.Rejected;
        if (resubmitted)
        {
            profile.VerificationStatus = ProviderVerificationStatus.PendingReview;
            profile.VerificationRejectionReason = null;
            profile.VerificationReviewedAt = null;
            profile.VerificationReviewedByUserId = null;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(storedFileName, cancellationToken);
            throw;
        }

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.DocumentUploaded,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderVerificationDocument),
            EntityId = document.Id.ToString(),
            Message = "Provider uploaded a professional verification document.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = profile.Id,
                ["documentType"] = type.ToString(),
                ["originalFileName"] = validation.SafeOriginalFileName,
                ["contentType"] = validation.CanonicalContentType,
                ["fileSizeBytes"] = validation.FileSizeBytes,
                ["resubmitted"] = resubmitted
            }
        }, cancellationToken);

        return await GetMineAsync(providerUserId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderVerificationDto>> DeleteMineAsync(
        string providerUserId,
        int documentId,
        CancellationToken cancellationToken)
    {
        var document = await _db.ProviderVerificationDocuments
            .Include(d => d.ProviderProfile)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return ServiceResult<ProviderVerificationDto>.NotFound("Document not found.");
        }

        if (document.ProviderProfile.UserId != providerUserId)
        {
            await _audit.RecordAsync(DeniedDocumentAccess(
                documentId,
                "Provider attempted to delete another provider's document."), cancellationToken);
            return ServiceResult<ProviderVerificationDto>.Forbidden(
                "You can only manage your own verification documents.");
        }

        if (document.ReviewStatus != VerificationDocumentStatus.Pending)
        {
            return ServiceResult<ProviderVerificationDto>.Conflict(
                "Reviewed documents cannot be deleted.");
        }

        var stored = document.StoredFileName;
        _db.ProviderVerificationDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(stored, cancellationToken);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.DocumentDeleted,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderVerificationDocument),
            EntityId = documentId.ToString(),
            Message = "Provider deleted a pending verification document.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = document.ProviderProfileId,
                ["originalFileName"] = document.OriginalFileName
            }
        }, cancellationToken);

        return await GetMineAsync(providerUserId, cancellationToken);
    }

    public async Task<ServiceResult<DocumentDownloadResult>> DownloadAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var document = await _db.ProviderVerificationDocuments
            .AsNoTracking()
            .Include(d => d.ProviderProfile)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return ServiceResult<DocumentDownloadResult>.NotFound("Document not found.");
        }

        var isOwner = document.ProviderProfile.UserId == userId;
        if (!isAdmin && !isOwner)
        {
            await _audit.RecordAsync(DeniedDocumentAccess(
                documentId,
                "Unauthorized verification document download."), cancellationToken);
            return ServiceResult<DocumentDownloadResult>.Forbidden(
                "You are not allowed to download this document.");
        }

        Stream content;
        try
        {
            content = await _storage.OpenReadAsync(document.StoredFileName, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return ServiceResult<DocumentDownloadResult>.NotFound("Document not found.");
        }

        await _audit.RecordAsync(new AuditEntry
        {
            Category = isAdmin ? AuditCategories.Admin : AuditCategories.Provider,
            Action = isAdmin ? AuditActions.AdminDocumentViewed : AuditActions.DocumentDownloaded,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderVerificationDocument),
            EntityId = document.Id.ToString(),
            Message = isAdmin
                ? "Admin downloaded a verification document."
                : "Provider downloaded their verification document.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = document.ProviderProfileId,
                ["originalFileName"] = document.OriginalFileName,
                ["contentType"] = document.ContentType
            }
        }, cancellationToken);

        return ServiceResult<DocumentDownloadResult>.Success(new DocumentDownloadResult
        {
            Content = content,
            ContentType = document.ContentType,
            OriginalFileName = document.OriginalFileName
        });
    }

    public async Task<ServiceResult<PagedResult<AdminVerificationListItemDto>>> ListVerificationsAsync(
        AdminVerificationQuery query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = query.Normalize();
        var providers = _db.ProviderProfiles.AsNoTracking();

        if (TryParseVerificationStatus(query.VerificationStatus, out var verificationStatus))
        {
            providers = providers.Where(p => p.VerificationStatus == verificationStatus);
        }
        else if (!string.IsNullOrWhiteSpace(query.VerificationStatus))
        {
            return ServiceResult<PagedResult<AdminVerificationListItemDto>>.Validation(
                "verificationStatus",
                "Verification status is not valid.");
        }

        if (TryParseDocumentStatus(query.DocumentStatus, out var documentStatus))
        {
            providers = providers.Where(p =>
                p.Documents.Any(d => d.ReviewStatus == documentStatus));
        }
        else if (!string.IsNullOrWhiteSpace(query.DocumentStatus))
        {
            return ServiceResult<PagedResult<AdminVerificationListItemDto>>.Validation(
                "documentStatus",
                "Document status is not valid.");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            providers = providers.Where(p =>
                p.User.FullName.ToLower().Contains(term) ||
                (p.User.Email != null && p.User.Email.ToLower().Contains(term)) ||
                p.City.ToLower().Contains(term));
        }

        var pageResult = await providers
            .OrderBy(p => p.VerificationStatus)
            .ThenBy(p => p.User.FullName)
            .Select(p => new AdminVerificationListItemDto
            {
                ProviderProfileId = p.Id,
                UserId = p.UserId,
                FullName = p.User.FullName,
                Email = p.User.Email ?? string.Empty,
                City = p.City,
                YearsOfExperience = p.YearsOfExperience,
                VerificationStatus = p.VerificationStatus.ToString(),
                IsSuspended = p.IsSuspended,
                DocumentCount = p.Documents.Count,
                PendingDocumentCount = p.Documents.Count(
                    d => d.ReviewStatus == VerificationDocumentStatus.Pending),
                ApprovedDocumentCount = p.Documents.Count(
                    d => d.ReviewStatus == VerificationDocumentStatus.Approved),
                RejectedDocumentCount = p.Documents.Count(
                    d => d.ReviewStatus == VerificationDocumentStatus.Rejected),
                Services = p.ProviderServices
                    .OrderBy(ps => ps.Service.Name)
                    .Select(ps => ps.Service.Name)
                    .ToList()
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<AdminVerificationListItemDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<ProviderVerificationDto>> GetAdminDetailAsync(
        int providerProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await LoadProfileByIdAsync(providerProfileId, cancellationToken);
        return profile is null
            ? ServiceResult<ProviderVerificationDto>.NotFound("Provider not found.")
            : ServiceResult<ProviderVerificationDto>.Success(ToDto(profile));
    }

    public async Task<ServiceResult<VerificationDocumentDto>> ReviewDocumentAsync(
        int documentId,
        ReviewVerificationDocumentRequest request,
        string adminUserId,
        CancellationToken cancellationToken)
    {
        if (!TryParseDocumentStatus(request.Status, out var status) ||
            status == VerificationDocumentStatus.Pending)
        {
            return ServiceResult<VerificationDocumentDto>.Validation(
                "status",
                "Status must be Approved or Rejected.");
        }

        if (status == VerificationDocumentStatus.Rejected &&
            string.IsNullOrWhiteSpace(request.Note))
        {
            return ServiceResult<VerificationDocumentDto>.Validation(
                "note",
                "A review note is required when rejecting a document.");
        }

        var document = await _db.ProviderVerificationDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return ServiceResult<VerificationDocumentDto>.NotFound("Document not found.");
        }

        document.ReviewStatus = status;
        document.ReviewNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        document.ReviewedAt = DateTimeOffset.UtcNow;
        document.ReviewedByUserId = adminUserId;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Admin,
            Action = AuditActions.AdminDocumentReviewed,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderVerificationDocument),
            EntityId = document.Id.ToString(),
            Message = $"Admin reviewed a verification document as {status}.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = document.ProviderProfileId,
                ["status"] = status.ToString(),
                ["hasNote"] = document.ReviewNote is not null
            }
        }, cancellationToken);

        return ServiceResult<VerificationDocumentDto>.Success(ToDocumentDto(document));
    }

    public async Task<ServiceResult<ProviderVerificationDto>> DecideProviderAsync(
        int providerProfileId,
        ProviderVerificationDecisionRequest request,
        string adminUserId,
        CancellationToken cancellationToken)
    {
        if (!TryParseVerificationStatus(request.Status, out var status))
        {
            return ServiceResult<ProviderVerificationDto>.Validation(
                "status",
                "Status must be Approved, Rejected, or PendingReview.");
        }

        var profile = await _db.ProviderProfiles
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken);
        if (profile is null)
        {
            return ServiceResult<ProviderVerificationDto>.NotFound("Provider not found.");
        }

        if (status == ProviderVerificationStatus.Approved)
        {
            var hasApprovedDocument = profile.Documents.Any(
                d => d.ReviewStatus == VerificationDocumentStatus.Approved);
            if (!hasApprovedDocument)
            {
                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Admin,
                    Action = AuditActions.ProviderApproved,
                    Outcome = AuditOutcomes.Denied,
                    EntityType = nameof(ProviderProfile),
                    EntityId = profile.Id.ToString(),
                    Message = "Provider approval denied because no professional document is approved."
                }, cancellationToken);

                return ServiceResult<ProviderVerificationDto>.Conflict(
                    "A provider cannot be approved without at least one approved professional document.");
            }

            profile.VerificationStatus = ProviderVerificationStatus.Approved;
            profile.VerificationRejectionReason = null;
            profile.VerificationReviewedAt = DateTimeOffset.UtcNow;
            profile.VerificationReviewedByUserId = adminUserId;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new AuditEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.ProviderApproved,
                Outcome = AuditOutcomes.Success,
                EntityType = nameof(ProviderProfile),
                EntityId = profile.Id.ToString(),
                Message = "Admin approved provider professional verification."
            }, cancellationToken);
        }
        else if (status == ProviderVerificationStatus.Rejected)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return ServiceResult<ProviderVerificationDto>.Validation(
                    "reason",
                    "A reason is required when rejecting a provider.");
            }

            profile.VerificationStatus = ProviderVerificationStatus.Rejected;
            profile.VerificationRejectionReason = request.Reason.Trim();
            profile.VerificationReviewedAt = DateTimeOffset.UtcNow;
            profile.VerificationReviewedByUserId = adminUserId;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new AuditEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.ProviderRejected,
                Outcome = AuditOutcomes.Success,
                EntityType = nameof(ProviderProfile),
                EntityId = profile.Id.ToString(),
                Message = "Admin rejected provider professional verification.",
                Details = new Dictionary<string, object?>
                {
                    ["reasonLength"] = profile.VerificationRejectionReason.Length
                }
            }, cancellationToken);
        }
        else
        {
            profile.VerificationStatus = ProviderVerificationStatus.PendingReview;
            profile.VerificationRejectionReason = null;
            profile.VerificationReviewedAt = null;
            profile.VerificationReviewedByUserId = null;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await GetAdminDetailAsync(providerProfileId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderVerificationDto>> SetSuspensionAsync(
        int providerProfileId,
        SetProviderSuspensionRequest request,
        string adminUserId,
        CancellationToken cancellationToken)
    {
        if (request.Suspended && string.IsNullOrWhiteSpace(request.Reason))
        {
            return ServiceResult<ProviderVerificationDto>.Validation(
                "reason",
                "A reason is required when suspending a provider.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var profile = await _db.ProviderProfiles
                    .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken);

                if (profile is null)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    return ServiceResult<ProviderVerificationDto>.NotFound("Provider not found.");
                }

                var rejectedPending = 0;
                if (request.Suspended)
                {
                    profile.IsSuspended = true;
                    profile.SuspensionReason = request.Reason!.Trim();
                    profile.SuspendedAt = DateTimeOffset.UtcNow;
                    profile.SuspendedByUserId = adminUserId;

                    var pending = await _db.Offers
                        .Where(o => o.ProviderId == profile.UserId && o.Status == OfferStatus.Pending)
                        .ToListAsync(cancellationToken);
                    foreach (var offer in pending)
                    {
                        offer.Status = OfferStatus.Rejected;
                    }

                    rejectedPending = pending.Count;
                }
                else
                {
                    profile.IsSuspended = false;
                    profile.SuspensionReason = null;
                    profile.SuspendedAt = null;
                    profile.SuspendedByUserId = null;
                }

                await _db.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                await _audit.RecordAsync(new AuditEntry
                {
                    Category = AuditCategories.Admin,
                    Action = request.Suspended
                        ? AuditActions.ProviderSuspended
                        : AuditActions.ProviderReactivated,
                    Outcome = AuditOutcomes.Success,
                    EntityType = nameof(ProviderProfile),
                    EntityId = profile.Id.ToString(),
                    Message = request.Suspended
                        ? "Admin suspended a provider."
                        : "Admin reactivated a provider.",
                    Details = new Dictionary<string, object?>
                    {
                        ["pendingOffersRejected"] = rejectedPending,
                        ["verificationStatus"] = profile.VerificationStatus.ToString()
                    }
                }, cancellationToken);

                return await GetAdminDetailAsync(providerProfileId, cancellationToken);
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        });
    }

    private Task<ProviderProfile?> LoadProfileByUserAsync(
        string userId,
        CancellationToken cancellationToken) =>
        _db.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    private Task<ProviderProfile?> LoadProfileByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _db.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    private static ProviderVerificationDto ToDto(ProviderProfile profile) => new()
    {
        ProviderProfileId = profile.Id,
        UserId = profile.UserId,
        FullName = profile.User.FullName,
        Email = profile.User.Email ?? string.Empty,
        City = profile.City,
        YearsOfExperience = profile.YearsOfExperience,
        Bio = profile.Bio,
        VerificationStatus = profile.VerificationStatus.ToString(),
        VerificationRejectionReason = profile.VerificationRejectionReason,
        VerificationReviewedAt = profile.VerificationReviewedAt,
        IsSuspended = profile.IsSuspended,
        SuspensionReason = profile.SuspensionReason,
        SuspendedAt = profile.SuspendedAt,
        AverageRating = profile.AverageRating,
        ReviewCount = profile.ReviewCount,
        Services = profile.ProviderServices
            .OrderBy(ps => ps.Service.Name)
            .Select(ps => ps.Service.Name)
            .ToList(),
        Documents = profile.Documents
            .OrderByDescending(d => d.UploadedAt)
            .Select(ToDocumentDto)
            .ToList(),
        HasApprovedDocument = profile.Documents.Any(
            d => d.ReviewStatus == VerificationDocumentStatus.Approved)
    };

    private static VerificationDocumentDto ToDocumentDto(ProviderVerificationDocument document) =>
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
            ReviewedAt = document.ReviewedAt
        };

    private static AuditEntry DeniedDocumentAccess(int documentId, string message) => new()
    {
        Category = AuditCategories.Admin,
        Action = AuditActions.AdminDocumentViewed,
        Outcome = AuditOutcomes.Denied,
        EntityType = nameof(ProviderVerificationDocument),
        EntityId = documentId.ToString(),
        Message = message
    };

    private static bool TryParseVerificationStatus(
        string? value,
        out ProviderVerificationStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static bool TryParseDocumentStatus(
        string? value,
        out VerificationDocumentStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);
}
