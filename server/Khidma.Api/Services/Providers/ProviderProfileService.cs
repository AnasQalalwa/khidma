using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Providers;
using Khidma.Api.Contracts.Schedule;
using Khidma.Api.Contracts.Reviews;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Infrastructure;
using Khidma.Api.Auth;
using Khidma.Api.Services.Audit;
using Khidma.Api.Services.Documents;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Providers;

public sealed class ProviderProfileService : IProviderProfileService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IProviderDocumentStorage _storage;

    public ProviderProfileService(
        AppDbContext db,
        IAuditService audit,
        IProviderDocumentStorage storage)
    {
        _db = db;
        _audit = audit;
        _storage = storage;
    }

    public async Task<ServiceResult<ProviderMeDto>> GetMeAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var dto = await LoadMeAsync(userId, cancellationToken);
        return dto is null
            ? ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.")
            : ServiceResult<ProviderMeDto>.Success(dto);
    }

    public async Task<ServiceResult<ProviderMeDto>> UpdateMeAsync(
        string userId,
        UpdateProviderProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
        }

        if (request.Latitude.HasValue != request.Longitude.HasValue)
        {
            return ServiceResult<ProviderMeDto>.Validation(
                "latitude",
                "Set both map coordinates, or leave both empty.");
        }

        var city = request.City.Trim();
        if (profile.IsLocationLocked && !SameLocation(profile, city, request.Latitude, request.Longitude))
        {
            return ServiceResult<ProviderMeDto>.Conflict(
                "Your approved city and map pin cannot be changed directly. Submit a location change for admin review.");
        }

        if (!profile.IsLocationLocked)
        {
            profile.City = city;
            profile.Latitude = request.Latitude;
            profile.Longitude = request.Longitude;
        }

        profile.YearsOfExperience = request.YearsOfExperience;
        profile.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        profile.User.PhoneNumber = PhoneRules.Normalize(request.PhoneNumber);

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.ProviderProfileUpdated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfile),
            EntityId = profile.Id.ToString(),
            Message = "Provider updated their profile.",
            Details = new Dictionary<string, object?>
            {
                ["city"] = profile.City,
                ["latitude"] = profile.Latitude,
                ["longitude"] = profile.Longitude,
                ["yearsOfExperience"] = profile.YearsOfExperience,
                ["locationLocked"] = profile.IsLocationLocked
            }
        }, cancellationToken);
        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderMeDto>> ReplaceServicesAsync(
        string userId,
        ReplaceProviderServicesRequest request,
        CancellationToken cancellationToken)
    {
        var serviceIds = request.ServiceIds.Distinct().ToList();
        var existingCount = await _db.Services
            .CountAsync(s => serviceIds.Contains(s.Id), cancellationToken);

        if (existingCount != serviceIds.Count)
        {
            return ServiceResult<ProviderMeDto>.Validation(
                "serviceIds",
                "One or more selected services do not exist.");
        }

        var profile = await _db.ProviderProfiles
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
        }

        if (profile.IsLocationLocked)
        {
            var currentIds = profile.ProviderServices.Select(ps => ps.ServiceId).ToHashSet();
            var additions = serviceIds.Where(id => !currentIds.Contains(id)).ToList();
            if (additions.Count > 0)
            {
                return ServiceResult<ProviderMeDto>.Conflict(
                    "Adding a service after approval requires professional proof and admin review.");
            }
        }

        _db.ProviderServices.RemoveRange(profile.ProviderServices);
        foreach (var serviceId in serviceIds)
        {
            profile.ProviderServices.Add(new ProviderService
            {
                ProviderProfileId = profile.Id,
                ServiceId = serviceId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.ProviderServicesUpdated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfile),
            EntityId = profile.Id.ToString(),
            Message = profile.IsLocationLocked
                ? "Provider removed offered services."
                : "Provider updated offered services.",
            Details = new Dictionary<string, object?>
            {
                ["serviceCount"] = serviceIds.Count
            }
        }, cancellationToken);
        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderMeDto>> RequestLocationChangeAsync(
        string userId,
        RequestLocationChangeRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .Include(p => p.ChangeRequests)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
        }

        if (!profile.IsLocationLocked)
        {
            return ServiceResult<ProviderMeDto>.Conflict(
                "Save your city and map pin on your profile until an admin approves your account.");
        }

        if (request.Latitude.HasValue != request.Longitude.HasValue)
        {
            return ServiceResult<ProviderMeDto>.Validation(
                "latitude",
                "Set both map coordinates, or leave both empty.");
        }

        var city = request.City.Trim();
        if (SameLocation(profile, city, request.Latitude, request.Longitude))
        {
            return ServiceResult<ProviderMeDto>.Validation(
                "city",
                "This is already your approved location.");
        }

        if (profile.ChangeRequests.Any(c =>
                c.Type == ProviderChangeRequestType.Location &&
                c.Status == ProviderChangeRequestStatus.Pending))
        {
            return ServiceResult<ProviderMeDto>.Conflict(
                "A location change is already waiting for admin review.");
        }

        var change = new ProviderProfileChangeRequest
        {
            ProviderProfileId = profile.Id,
            Type = ProviderChangeRequestType.Location,
            Status = ProviderChangeRequestStatus.Pending,
            RequestedCity = city,
            RequestedLatitude = request.Latitude,
            RequestedLongitude = request.Longitude,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ProviderProfileChangeRequests.Add(change);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.ProviderChangeRequested,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfileChangeRequest),
            EntityId = change.Id.ToString(),
            Message = "Provider requested a location change.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = profile.Id,
                ["requestedCity"] = city
            }
        }, cancellationToken);

        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderMeDto>> RequestServiceAdditionAsync(
        string userId,
        int serviceId,
        string documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<VerificationDocumentType>(documentType, ignoreCase: true, out var type) ||
            !Enum.IsDefined(type))
        {
            return ServiceResult<ProviderMeDto>.Validation(
                "documentType",
                "Document type is not valid.");
        }

        var validation = DocumentFileValidator.Validate(file);
        if (!validation.Succeeded)
        {
            return ServiceResult<ProviderMeDto>.Validation("file", validation.Error!);
        }

        var serviceExists = await _db.Services.AnyAsync(s => s.Id == serviceId, cancellationToken);
        if (!serviceExists)
        {
            return ServiceResult<ProviderMeDto>.Validation(
                "serviceId",
                "That service does not exist.");
        }

        var profile = await _db.ProviderProfiles
            .Include(p => p.ProviderServices)
            .Include(p => p.ChangeRequests)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
        }

        if (!profile.IsLocationLocked)
        {
            return ServiceResult<ProviderMeDto>.Conflict(
                "Choose services on your profile until an admin approves your account, and upload professional proof there.");
        }

        if (profile.ProviderServices.Any(ps => ps.ServiceId == serviceId))
        {
            return ServiceResult<ProviderMeDto>.Conflict(
                "You already offer this service.");
        }

        if (profile.ChangeRequests.Any(c =>
                c.Type == ProviderChangeRequestType.AddService &&
                c.Status == ProviderChangeRequestStatus.Pending &&
                c.ServiceId == serviceId))
        {
            return ServiceResult<ProviderMeDto>.Conflict(
                "This service is already waiting for admin review.");
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
            ServiceId = serviceId,
            DocumentType = type,
            OriginalFileName = validation.SafeOriginalFileName,
            StoredFileName = storedFileName,
            ContentType = validation.CanonicalContentType,
            FileSizeBytes = validation.FileSizeBytes,
            UploadedAt = DateTimeOffset.UtcNow,
            ReviewStatus = VerificationDocumentStatus.Pending
        };
        _db.ProviderVerificationDocuments.Add(document);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(storedFileName, cancellationToken);
            throw;
        }

        var change = new ProviderProfileChangeRequest
        {
            ProviderProfileId = profile.Id,
            Type = ProviderChangeRequestType.AddService,
            Status = ProviderChangeRequestStatus.Pending,
            ServiceId = serviceId,
            ProofDocumentId = document.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ProviderProfileChangeRequests.Add(change);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.ProviderChangeRequested,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfileChangeRequest),
            EntityId = change.Id.ToString(),
            Message = "Provider requested to add a service with professional proof.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = profile.Id,
                ["serviceId"] = serviceId,
                ["documentId"] = document.Id
            }
        }, cancellationToken);

        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderMeDto>> UploadPhotoAsync(
        string userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = DocumentFileValidator.ValidateImage(file);
        if (!validation.Succeeded)
        {
            return ServiceResult<ProviderMeDto>.Validation("file", validation.Error!);
        }

        var profile = await _db.ProviderProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
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

        var previous = profile.PhotoStoredFileName;
        profile.PhotoStoredFileName = storedFileName;
        profile.PhotoContentType = validation.CanonicalContentType;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(storedFileName, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previous))
        {
            await _storage.DeleteAsync(previous, cancellationToken);
        }

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.ProviderPhotoUpdated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfile),
            EntityId = profile.Id.ToString(),
            Message = "Provider updated their profile photo."
        }, cancellationToken);

        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<ProviderMeDto>> DeletePhotoAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
        }

        if (string.IsNullOrWhiteSpace(profile.PhotoStoredFileName))
        {
            return ServiceResult<ProviderMeDto>.NotFound("No profile photo.");
        }

        var stored = profile.PhotoStoredFileName;
        profile.PhotoStoredFileName = null;
        profile.PhotoContentType = null;
        await _db.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(stored, cancellationToken);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Provider,
            Action = AuditActions.ProviderPhotoUpdated,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfile),
            EntityId = profile.Id.ToString(),
            Message = "Provider removed their profile photo."
        }, cancellationToken);

        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<DocumentDownloadResult>> GetPhotoAsync(
        int providerProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken);

        if (profile is null || string.IsNullOrWhiteSpace(profile.PhotoStoredFileName))
        {
            return ServiceResult<DocumentDownloadResult>.NotFound("No profile photo.");
        }

        Stream content;
        try
        {
            content = await _storage.OpenReadAsync(profile.PhotoStoredFileName, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return ServiceResult<DocumentDownloadResult>.NotFound("No profile photo.");
        }

        var extension = Path.GetExtension(profile.PhotoStoredFileName);
        return ServiceResult<DocumentDownloadResult>.Success(new DocumentDownloadResult
        {
            Content = content,
            ContentType = profile.PhotoContentType ?? "image/jpeg",
            OriginalFileName = $"profile{extension}"
        });
    }

    public async Task<ServiceResult<ProviderChangeRequestDto>> ReviewChangeRequestAsync(
        int changeRequestId,
        ReviewProviderChangeRequest request,
        string adminUserId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProviderChangeRequestStatus>(request.Status, ignoreCase: true, out var status) ||
            status == ProviderChangeRequestStatus.Pending ||
            !Enum.IsDefined(status))
        {
            return ServiceResult<ProviderChangeRequestDto>.Validation(
                "status",
                "Status must be Approved or Rejected.");
        }

        if (status == ProviderChangeRequestStatus.Rejected && string.IsNullOrWhiteSpace(request.Note))
        {
            return ServiceResult<ProviderChangeRequestDto>.Validation(
                "note",
                "A review note is required when rejecting a profile change.");
        }

        var change = await _db.ProviderProfileChangeRequests
            .Include(c => c.ProviderProfile)
                .ThenInclude(p => p.ProviderServices)
            .Include(c => c.ProofDocument)
            .Include(c => c.Service)
            .FirstOrDefaultAsync(c => c.Id == changeRequestId, cancellationToken);

        if (change is null)
        {
            return ServiceResult<ProviderChangeRequestDto>.NotFound("Change request not found.");
        }

        if (change.Status != ProviderChangeRequestStatus.Pending)
        {
            return ServiceResult<ProviderChangeRequestDto>.Conflict(
                "This change request has already been reviewed.");
        }

        if (status == ProviderChangeRequestStatus.Approved)
        {
            var applied = await ApplyApprovedChangeAsync(change, cancellationToken);
            if (!applied.Succeeded)
            {
                return ServiceResult<ProviderChangeRequestDto>.Conflict(applied.Title, applied.Detail);
            }
        }

        change.Status = status;
        change.ReviewNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        change.ReviewedAt = DateTimeOffset.UtcNow;
        change.ReviewedByUserId = adminUserId;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Admin,
            Action = AuditActions.AdminChangeReviewed,
            Outcome = AuditOutcomes.Success,
            EntityType = nameof(ProviderProfileChangeRequest),
            EntityId = change.Id.ToString(),
            Message = $"Admin reviewed a provider profile change as {status}.",
            Details = new Dictionary<string, object?>
            {
                ["providerProfileId"] = change.ProviderProfileId,
                ["type"] = change.Type.ToString(),
                ["status"] = status.ToString()
            }
        }, cancellationToken);

        return ServiceResult<ProviderChangeRequestDto>.Success(ProviderChangeMapping.ToDto(change));
    }

    public async Task<ServiceResult<PublicProviderDto>> GetPublicAsync(
        int providerProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.WorkingHours)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.Category)
            .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<PublicProviderDto>.NotFound("Provider not found.");
        }

        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProviderId == profile.UserId)
            .OrderByDescending(r => r.Id)
            .Take(8)
            .Select(r => new PublicReviewDto
            {
                ReviewerFirstName = r.Customer.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<PublicProviderDto>.Success(new PublicProviderDto
        {
            Id = profile.Id,
            FullName = profile.User.FullName,
            City = profile.City,
            YearsOfExperience = profile.YearsOfExperience,
            Bio = profile.Bio,
            IsVerified = profile.VerificationStatus == ProviderVerificationStatus.Approved,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            HasPhoto = profile.HasPhoto,
            WorkingHours = profile.WorkingHours
                .OrderBy(h => h.DayOfWeek)
                .ThenBy(h => h.Hour)
                .Select(h => new WorkingHourDto
                {
                    DayOfWeek = h.DayOfWeek,
                    Hour = h.Hour
                })
                .ToList(),
            Services = profile.ProviderServices
                .OrderBy(ps => ps.Service.Category.Name)
                .ThenBy(ps => ps.Service.Name)
                .Select(ps => new ServiceDto
                {
                    Id = ps.Service.Id,
                    Name = ps.Service.Name,
                    Description = ps.Service.Description,
                    CategoryId = ps.Service.CategoryId,
                    CategoryName = ps.Service.Category.Name,
                    HasImage = ps.Service.ImageStoredFileName != null
                })
                .ToList(),
            RecentReviews = reviews
                .Select(r => new PublicReviewDto
                {
                    ReviewerFirstName = FirstName(r.ReviewerFirstName),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToList()
        });
    }

    internal static async Task<ServiceResult<bool>> ApplyApprovedChangeAsync(
        ProviderProfileChangeRequest change,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (change.Type == ProviderChangeRequestType.Location)
        {
            if (string.IsNullOrWhiteSpace(change.RequestedCity))
            {
                return ServiceResult<bool>.Conflict("This location change is missing a city.");
            }

            change.ProviderProfile.City = change.RequestedCity.Trim();
            change.ProviderProfile.Latitude = change.RequestedLatitude;
            change.ProviderProfile.Longitude = change.RequestedLongitude;
            return ServiceResult<bool>.Success(true);
        }

        if (change.Type == ProviderChangeRequestType.AddService)
        {
            if (change.ServiceId is null)
            {
                return ServiceResult<bool>.Conflict("This service change is missing a service.");
            }

            if (change.ProofDocument is null ||
                change.ProofDocument.ReviewStatus != VerificationDocumentStatus.Approved)
            {
                return ServiceResult<bool>.Conflict(
                    "Approve the professional proof for this service before approving the service.");
            }

            if (!change.ProviderProfile.ProviderServices.Any(ps => ps.ServiceId == change.ServiceId))
            {
                change.ProviderProfile.ProviderServices.Add(new ProviderService
                {
                    ProviderProfileId = change.ProviderProfileId,
                    ServiceId = change.ServiceId.Value
                });
            }

            return ServiceResult<bool>.Success(true);
        }

        return ServiceResult<bool>.Conflict("Unsupported profile change type.");
    }

    private async Task<ProviderMeDto?> LoadMeAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.Category)
            .Include(p => p.ChangeRequests)
                .ThenInclude(c => c.Service)
            .Include(p => p.ChangeRequests)
                .ThenInclude(c => c.ProofDocument)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        return new ProviderMeDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.User.FullName,
            Email = profile.User.Email ?? string.Empty,
            PhoneNumber = profile.User.PhoneNumber ?? string.Empty,
            City = profile.City,
            Latitude = profile.Latitude,
            Longitude = profile.Longitude,
            YearsOfExperience = profile.YearsOfExperience,
            Bio = profile.Bio,
            VerificationStatus = profile.VerificationStatus.ToString(),
            IsSuspended = profile.IsSuspended,
            SuspensionReason = profile.SuspensionReason,
            VerificationRejectionReason = profile.VerificationRejectionReason,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            HasPhoto = profile.HasPhoto,
            CanEditLocation = !profile.IsLocationLocked,
            CanEditServices = !profile.IsLocationLocked,
            PendingChanges = ProviderChangeMapping.PendingOf(profile),
            Services = profile.ProviderServices
                .OrderBy(ps => ps.Service.Category.Name)
                .ThenBy(ps => ps.Service.Name)
                .Select(ps => new ServiceDto
                {
                    Id = ps.Service.Id,
                    Name = ps.Service.Name,
                    Description = ps.Service.Description,
                    CategoryId = ps.Service.CategoryId,
                    CategoryName = ps.Service.Category.Name,
                    HasImage = ps.Service.ImageStoredFileName != null
                })
                .ToList()
        };
    }

    private static string FirstName(string fullName)
    {
        var trimmed = fullName.Trim();
        var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? trimmed : trimmed[..space];
    }

    private static bool SameLocation(
        ProviderProfile profile,
        string city,
        decimal? latitude,
        decimal? longitude) =>
        string.Equals(profile.City.Trim(), city.Trim(), StringComparison.OrdinalIgnoreCase) &&
        profile.Latitude == latitude &&
        profile.Longitude == longitude;
}
