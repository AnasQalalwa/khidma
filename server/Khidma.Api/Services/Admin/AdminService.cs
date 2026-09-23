using Khidma.Api.Auth;
using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Audit;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Infrastructure;
using Khidma.Api.Services.Audit;
using Khidma.Api.Services.Documents;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Admin;

public sealed partial class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;
    private readonly IProviderDocumentStorage _storage;

    public AdminService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditService audit,
        IProviderDocumentStorage storage)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
        _storage = storage;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        return new AdminStatsDto
        {
            TotalUsers = await _db.Users.CountAsync(cancellationToken),
            Customers = await _db.CustomerProfiles.CountAsync(cancellationToken),
            Providers = await _db.ProviderProfiles.CountAsync(cancellationToken),
            PendingVerification = await _db.ProviderProfiles.CountAsync(
                p => p.VerificationStatus == ProviderVerificationStatus.PendingReview,
                cancellationToken),
            PendingProviders = await _db.ProviderProfiles.CountAsync(
                p => p.VerificationStatus == ProviderVerificationStatus.PendingReview,
                cancellationToken),
            ApprovedProviders = await _db.ProviderProfiles.CountAsync(
                p => p.VerificationStatus == ProviderVerificationStatus.Approved,
                cancellationToken),
            SuspendedProviders = await _db.ProviderProfiles.CountAsync(
                p => p.IsSuspended,
                cancellationToken),
            PendingDocuments = await _db.ProviderVerificationDocuments.CountAsync(
                d => d.ReviewStatus == VerificationDocumentStatus.Pending,
                cancellationToken),
            RejectedDocuments = await _db.ProviderVerificationDocuments.CountAsync(
                d => d.ReviewStatus == VerificationDocumentStatus.Rejected,
                cancellationToken),
            Categories = await _db.Categories.CountAsync(cancellationToken),
            Services = await _db.Services.CountAsync(cancellationToken),
            PendingBookings = await _db.Bookings
                .CountAsync(b => b.Status == BookingStatus.Pending, cancellationToken),
            ActiveBookings = await _db.Bookings.CountAsync(
                b => b.Status == BookingStatus.Scheduled ||
                     b.Status == BookingStatus.InProgress,
                cancellationToken),
            CompletedBookings = await _db.Bookings
                .CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken),
            AuditEventsLast24h = await _db.AuditLogs
                .CountAsync(a => a.CreatedAt >= since, cancellationToken)
        };
    }

    public async Task<AdminAttentionDto> GetAttentionAsync(CancellationToken cancellationToken)
    {
        var items = new List<AdminAttentionItemDto>();

        var pendingReview = await _db.ProviderProfiles.CountAsync(
            p => p.VerificationStatus == ProviderVerificationStatus.PendingReview,
            cancellationToken);
        if (pendingReview > 0)
        {
            items.Add(new AdminAttentionItemDto
            {
                Kind = "PendingVerification",
                Title = "Providers waiting for review",
                Detail = $"{pendingReview} provider{(pendingReview == 1 ? "" : "s")} awaiting professional verification.",
                Href = "/admin/verifications?verificationStatus=PendingReview"
            });
        }

        var pendingDocs = await _db.ProviderVerificationDocuments.CountAsync(
            d => d.ReviewStatus == VerificationDocumentStatus.Pending,
            cancellationToken);
        if (pendingDocs > 0)
        {
            items.Add(new AdminAttentionItemDto
            {
                Kind = "PendingDocuments",
                Title = "Documents waiting for review",
                Detail = $"{pendingDocs} professional document{(pendingDocs == 1 ? "" : "s")} still pending.",
                Href = "/admin/verifications?documentStatus=Pending"
            });
        }

        var pendingChanges = await _db.ProviderProfileChangeRequests.CountAsync(
            c => c.Status == ProviderChangeRequestStatus.Pending,
            cancellationToken);
        if (pendingChanges > 0)
        {
            items.Add(new AdminAttentionItemDto
            {
                Kind = "PendingProfileChanges",
                Title = "Profile changes waiting for review",
                Detail = $"{pendingChanges} location or service change{(pendingChanges == 1 ? "" : "s")} need approval.",
                Href = "/admin/verifications?hasPendingChanges=true"
            });
        }

        var rejectedDocs = await _db.ProviderVerificationDocuments.CountAsync(
            d => d.ReviewStatus == VerificationDocumentStatus.Rejected,
            cancellationToken);
        if (rejectedDocs > 0)
        {
            items.Add(new AdminAttentionItemDto
            {
                Kind = "RejectedDocuments",
                Title = "Rejected documents",
                Detail = $"{rejectedDocs} document{(rejectedDocs == 1 ? "" : "s")} were rejected and may need a resubmission.",
                Href = "/admin/verifications?documentStatus=Rejected"
            });
        }

        var suspended = await _db.ProviderProfiles.CountAsync(p => p.IsSuspended, cancellationToken);
        if (suspended > 0)
        {
            items.Add(new AdminAttentionItemDto
            {
                Kind = "SuspendedProviders",
                Title = "Suspended providers",
                Detail = $"{suspended} provider{(suspended == 1 ? "" : "s")} cannot receive new work.",
                Href = "/admin/providers?suspended=true"
            });
        }

        var denied = await _db.AuditLogs.CountAsync(
            a => a.Outcome == AuditOutcomes.Denied &&
                 a.CreatedAt >= DateTimeOffset.UtcNow.AddHours(-24),
            cancellationToken);
        if (denied > 0)
        {
            items.Add(new AdminAttentionItemDto
            {
                Kind = "DeniedActions",
                Title = "Denied actions in the last 24 hours",
                Detail = $"{denied} security or policy denial{(denied == 1 ? "" : "s")} were recorded.",
                Href = "/admin/audit?outcome=Denied"
            });
        }

        return new AdminAttentionDto { Items = items };
    }

    public async Task<ServiceResult<PagedResult<AdminProviderListItemDto>>> GetProvidersAsync(
        AdminProviderQuery query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = query.Normalize();
        var providers = _db.ProviderProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.VerificationStatus))
        {
            if (!Enum.TryParse<ProviderVerificationStatus>(
                    query.VerificationStatus,
                    ignoreCase: true,
                    out var status) ||
                !Enum.IsDefined(status))
            {
                return ServiceResult<PagedResult<AdminProviderListItemDto>>.Validation(
                    "verificationStatus",
                    "Verification status is not valid.");
            }

            providers = providers.Where(p => p.VerificationStatus == status);
        }

        if (query.Suspended is not null)
        {
            providers = providers.Where(p => p.IsSuspended == query.Suspended);
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
            .OrderBy(p => p.User.FullName)
            .Select(p => new AdminProviderListItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FullName = p.User.FullName,
                Email = p.User.Email ?? string.Empty,
                City = p.City,
                VerificationStatus = p.VerificationStatus.ToString(),
                IsSuspended = p.IsSuspended,
                SuspensionReason = p.SuspensionReason,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                DocumentCount = p.Documents.Count,
                ApprovedDocumentCount = p.Documents.Count(
                    d => d.ReviewStatus == VerificationDocumentStatus.Approved),
                Services = p.ProviderServices
                    .OrderBy(ps => ps.Service.Name)
                    .Select(ps => ps.Service.Name)
                    .ToList()
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<AdminProviderListItemDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<PagedResult<AdminUserListItemDto>>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = query.Normalize();
        var users = from user in _db.Users.AsNoTracking()
                    join userRole in _db.UserRoles on user.Id equals userRole.UserId
                    join role in _db.Roles on userRole.RoleId equals role.Id
                    select new { user, Role = role.Name ?? string.Empty };

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim();
            users = users.Where(row => row.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            users = users.Where(row =>
                row.user.FullName.ToLower().Contains(term) ||
                (row.user.Email != null && row.user.Email.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.ProviderVerificationStatus) ||
            query.Suspended is not null)
        {
            users = users.Where(row => row.user.ProviderProfile != null);
            if (!string.IsNullOrWhiteSpace(query.ProviderVerificationStatus))
            {
                if (!Enum.TryParse<ProviderVerificationStatus>(
                        query.ProviderVerificationStatus,
                        ignoreCase: true,
                        out var status) ||
                    !Enum.IsDefined(status))
                {
                    return ServiceResult<PagedResult<AdminUserListItemDto>>.Validation(
                        "providerVerificationStatus",
                        "Verification status is not valid.");
                }

                users = users.Where(row =>
                    row.user.ProviderProfile!.VerificationStatus == status);
            }

            if (query.Suspended is not null)
            {
                users = users.Where(row =>
                    row.user.ProviderProfile!.IsSuspended == query.Suspended);
            }
        }

        var pageResult = await users
            .OrderByDescending(row => row.user.CreatedAt)
            .Select(row => new AdminUserListItemDto
            {
                UserId = row.user.Id,
                FullName = row.user.FullName,
                Email = row.user.Email ?? string.Empty,
                Role = row.Role,
                CreatedAt = row.user.CreatedAt,
                LastLoginAt = row.user.LastLoginAt,
                ProviderProfileId = row.user.ProviderProfile == null
                    ? null
                    : row.user.ProviderProfile.Id,
                VerificationStatus = row.user.ProviderProfile == null
                    ? null
                    : row.user.ProviderProfile.VerificationStatus.ToString(),
                IsSuspended = row.user.ProviderProfile == null
                    ? null
                    : row.user.ProviderProfile.IsSuspended,
                AverageRating = row.user.ProviderProfile == null
                    ? null
                    : row.user.ProviderProfile.AverageRating,
                ReviewCount = row.user.ProviderProfile == null
                    ? null
                    : row.user.ProviderProfile.ReviewCount,
                City = row.user.ProviderProfile != null
                    ? row.user.ProviderProfile.City
                    : row.user.CustomerProfile != null
                        ? row.user.CustomerProfile.City
                        : null
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<AdminUserListItemDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<AdminUserDetailDto>> GetUserAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.CustomerProfile)
            .Include(u => u.ProviderProfile)
                .ThenInclude(p => p!.ProviderServices)
                    .ThenInclude(ps => ps.Service)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<AdminUserDetailDto>.NotFound("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        var recentLogs = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActorUserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);
        var recentSummaries = await AuditSummaryBuilder.BuildAsync(
            _db,
            recentLogs,
            cancellationToken);
        var recentAudit = recentLogs
            .Select(log => ToAuditListItem(log, recentSummaries[log.Id]))
            .ToList();

        var dto = new AdminUserDetailDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            City = user.ProviderProfile?.City ?? user.CustomerProfile?.City,
            RecentAuditEvents = recentAudit
        };

        if (role == AppRoles.Customer)
        {
            dto.BookingCount = await _db.Bookings.CountAsync(
                b => b.CustomerId == user.Id,
                cancellationToken);
            dto.ReviewCount = await _db.Reviews.CountAsync(
                r => r.CustomerId == user.Id,
                cancellationToken);
        }
        else if (role == AppRoles.Provider && user.ProviderProfile is not null)
        {
            var profile = user.ProviderProfile;
            dto.ProviderProfileId = profile.Id;
            dto.VerificationStatus = profile.VerificationStatus.ToString();
            dto.IsSuspended = profile.IsSuspended;
            dto.SuspensionReason = profile.SuspensionReason;
            dto.AverageRating = profile.AverageRating;
            dto.ReviewCount = profile.ReviewCount;
            dto.ActiveBookingCount = await _db.Bookings.CountAsync(
                b => b.ProviderId == user.Id &&
                     (b.Status == BookingStatus.Scheduled ||
                      b.Status == BookingStatus.InProgress),
                cancellationToken);
            dto.CompletedBookingCount = await _db.Bookings.CountAsync(
                b => b.ProviderId == user.Id && b.Status == BookingStatus.Completed,
                cancellationToken);
            dto.BookingCount = await _db.Bookings.CountAsync(
                b => b.ProviderId == user.Id,
                cancellationToken);
            dto.Services = profile.ProviderServices
                .OrderBy(ps => ps.Service.Name)
                .Select(ps => ps.Service.Name)
                .ToList();
        }

        return ServiceResult<AdminUserDetailDto>.Success(dto);
    }

    public async Task<ServiceResult<PagedResult<AuditLogListItemDto>>> GetAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = query.Normalize(25, 100);
        var logs = _db.AuditLogs.AsNoTracking();

        if (query.From is not null)
        {
            logs = logs.Where(a => a.CreatedAt >= query.From);
        }

        if (query.To is not null)
        {
            logs = logs.Where(a => a.CreatedAt <= query.To);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
        {
            logs = logs.Where(a => a.ActorUserId == query.ActorUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorEmail))
        {
            var email = query.ActorEmail.Trim().ToLower();
            logs = logs.Where(a => a.ActorEmail != null && a.ActorEmail.ToLower().Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            logs = logs.Where(a => a.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(a => a.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logs = logs.Where(a => a.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            logs = logs.Where(a => a.EntityId == query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            logs = logs.Where(a => a.Outcome == query.Outcome);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            logs = logs.Where(a =>
                a.Action.ToLower().Contains(term) ||
                a.Category.ToLower().Contains(term) ||
                (a.Message != null && a.Message.ToLower().Contains(term)) ||
                (a.ActorEmail != null && a.ActorEmail.ToLower().Contains(term)) ||
                (a.EntityType != null && a.EntityType.ToLower().Contains(term)) ||
                (a.EntityId != null && a.EntityId.ToLower().Contains(term)));
        }

        if (query.HideAuth)
        {
            logs = logs.Where(a =>
                a.Action != AuditActions.LoginSucceeded &&
                a.Action != AuditActions.Logout);
        }

        var pageResult = await logs
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        var summaries = await AuditSummaryBuilder.BuildAsync(
            _db,
            pageResult.Items,
            cancellationToken);

        return ServiceResult<PagedResult<AuditLogListItemDto>>.Success(new PagedResult<AuditLogListItemDto>
        {
            Items = pageResult.Items
                .Select(log => ToAuditListItem(log, summaries[log.Id]))
                .ToList(),
            Page = pageResult.Page,
            PageSize = pageResult.PageSize,
            TotalCount = pageResult.TotalCount,
            TotalPages = pageResult.TotalPages
        });
    }

    public async Task<ServiceResult<AuditLogDetailDto>> GetAuditLogAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var log = await _db.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (log is null)
        {
            return ServiceResult<AuditLogDetailDto>.NotFound("Audit log not found.");
        }

        return ServiceResult<AuditLogDetailDto>.Success(new AuditLogDetailDto
        {
            Id = log.Id,
            CreatedAt = log.CreatedAt,
            ActorUserId = log.ActorUserId,
            ActorEmail = log.ActorEmail,
            ActorRole = log.ActorRole,
            Category = log.Category,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Outcome = log.Outcome,
            Message = log.Message,
            Summary = await AuditSummaryBuilder.BuildAsync(_db, log, cancellationToken),
            DetailsJson = log.DetailsJson,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            CorrelationId = log.CorrelationId
        });
    }

    public AuditFilterOptionsDto GetAuditFilterOptions() =>
        AuditSummaryBuilder.FilterOptions();

    public async Task<AuditSummaryDto> GetAuditSummaryAsync(CancellationToken cancellationToken)
    {
        var startOfDay = DateTimeOffset.UtcNow.Date;
        return new AuditSummaryDto
        {
            EventsToday = await _db.AuditLogs.CountAsync(
                a => a.CreatedAt >= startOfDay,
                cancellationToken),
            DeniedActions = await _db.AuditLogs.CountAsync(
                a => a.Outcome == AuditOutcomes.Denied,
                cancellationToken),
            AdminActions = await _db.AuditLogs.CountAsync(
                a => a.Category == AuditCategories.Admin,
                cancellationToken),
            ProviderVerificationEvents = await _db.AuditLogs.CountAsync(
                a => a.Action.StartsWith("Admin.Provider") ||
                     a.Action.StartsWith("Provider.Document") ||
                     a.Action == AuditActions.AdminDocumentReviewed ||
                     a.Action == AuditActions.AdminDocumentViewed,
                cancellationToken)
        };
    }

    public async Task<ServiceResult<CategoryDto>> CreateCategoryAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await NameTakenAsync(name, null, cancellationToken))
        {
            return ServiceResult<CategoryDto>.Conflict(
                "A category with this name already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description.Trim()
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.CategoryCreated,
            nameof(Category),
            category.Id.ToString(),
            "Admin created a category.",
            cancellationToken);
        return ServiceResult<CategoryDto>.Success(ToCategoryDto(category));
    }

    public async Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(
        int id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.NotFound("Category not found.");
        }

        var name = request.Name.Trim();
        if (await NameTakenAsync(name, id, cancellationToken))
        {
            return ServiceResult<CategoryDto>.Conflict(
                "A category with this name already exists.");
        }

        category.Name = name;
        category.Description = request.Description.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.CategoryUpdated,
            nameof(Category),
            category.Id.ToString(),
            "Admin updated a category.",
            cancellationToken);
        return ServiceResult<CategoryDto>.Success(ToCategoryDto(category));
    }

    public async Task<IReadOnlyList<CatalogServiceUsageDto>> GetCatalogUsageAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _db.Services
            .AsNoTracking()
            .Select(service => new
            {
                service.Id,
                ProviderCount = service.ProviderServices.Count,
                BookingCount = service.Bookings.Count,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CatalogServiceUsageDto
            {
                ServiceId = row.Id,
                ProviderCount = row.ProviderCount,
                BookingCount = row.BookingCount,
                DeleteBlockReason = ServiceDeleteBlockReason(row.ProviderCount, row.BookingCount),
            })
            .ToList();
    }

    public async Task<ServiceResult<bool>> DeleteCategoryAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            return ServiceResult<bool>.NotFound("Category not found.");
        }

        if (category.Services.Count > 0)
        {
            return ServiceResult<bool>.Conflict(
                CategoryDeleteBlockReason(category.Services.Select(service => service.Name)));
        }

        var image = category.ImageStoredFileName;
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(image))
        {
            await _storage.DeleteAsync(image, cancellationToken);
        }
        await RecordCatalogAsync(
            AuditActions.CategoryDeleted,
            nameof(Category),
            id.ToString(),
            "Admin deleted a category.",
            cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<ServiceDto>> CreateServiceAsync(
        SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(
            c => c.Id == request.CategoryId,
            cancellationToken);
        if (category is null)
        {
            return ServiceResult<ServiceDto>.Validation(
                "categoryId",
                "The selected category does not exist.");
        }

        var name = request.Name.Trim();
        if (await ServiceNameTakenAsync(name, request.CategoryId, null, cancellationToken))
        {
            return ServiceResult<ServiceDto>.Conflict(
                "A service with this name already exists in the category.");
        }

        var service = new Domain.Service
        {
            Name = name,
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.ServiceCreated,
            nameof(Domain.Service),
            service.Id.ToString(),
            "Admin created a service.",
            cancellationToken);

        return ServiceResult<ServiceDto>.Success(ToServiceDto(service, category.Name));
    }

    public async Task<ServiceResult<ServiceDto>> UpdateServiceAsync(
        int id,
        SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult<ServiceDto>.NotFound("Service not found.");
        }

        var category = await _db.Categories.FirstOrDefaultAsync(
            c => c.Id == request.CategoryId,
            cancellationToken);
        if (category is null)
        {
            return ServiceResult<ServiceDto>.Validation(
                "categoryId",
                "The selected category does not exist.");
        }

        var name = request.Name.Trim();
        if (await ServiceNameTakenAsync(name, request.CategoryId, id, cancellationToken))
        {
            return ServiceResult<ServiceDto>.Conflict(
                "A service with this name already exists in the category.");
        }

        service.Name = name;
        service.Description = request.Description.Trim();
        service.CategoryId = request.CategoryId;
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.ServiceUpdated,
            nameof(Domain.Service),
            service.Id.ToString(),
            "Admin updated a service.",
            cancellationToken);

        return ServiceResult<ServiceDto>.Success(ToServiceDto(service, category.Name));
    }

    public async Task<ServiceResult<bool>> DeleteServiceAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .Include(s => s.ProviderServices)
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service is null)
        {
            return ServiceResult<bool>.NotFound("Service not found.");
        }

        var blockReason = ServiceDeleteBlockReason(
            service.ProviderServices.Count,
            service.Bookings.Count);
        if (blockReason is not null)
        {
            return ServiceResult<bool>.Conflict(blockReason);
        }

        var image = service.ImageStoredFileName;
        _db.Services.Remove(service);
        await _db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(image))
        {
            await _storage.DeleteAsync(image, cancellationToken);
        }
        await RecordCatalogAsync(
            AuditActions.ServiceDeleted,
            nameof(Domain.Service),
            id.ToString(),
            "Admin deleted a service.",
            cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<CategoryDto>> SetCategoryImageAsync(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.NotFound("Category not found.");
        }

        var stored = await StoreCatalogImageAsync(file, cancellationToken);
        if (!stored.Succeeded)
        {
            return ServiceResult<CategoryDto>.Validation(stored.Errors);
        }

        var previous = category.ImageStoredFileName;
        category.ImageStoredFileName = stored.Value!.FileName;
        category.ImageContentType = stored.Value.ContentType;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(stored.Value.FileName, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previous))
        {
            await _storage.DeleteAsync(previous, cancellationToken);
        }

        await RecordCatalogAsync(
            AuditActions.CategoryUpdated,
            nameof(Category),
            category.Id.ToString(),
            "Admin updated a category image.",
            cancellationToken);
        return ServiceResult<CategoryDto>.Success(ToCategoryDto(category));
    }

    public async Task<ServiceResult<ServiceDto>> SetServiceImageAsync(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult<ServiceDto>.NotFound("Service not found.");
        }

        var stored = await StoreCatalogImageAsync(file, cancellationToken);
        if (!stored.Succeeded)
        {
            return ServiceResult<ServiceDto>.Validation(stored.Errors);
        }

        var previous = service.ImageStoredFileName;
        service.ImageStoredFileName = stored.Value!.FileName;
        service.ImageContentType = stored.Value.ContentType;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(stored.Value.FileName, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previous))
        {
            await _storage.DeleteAsync(previous, cancellationToken);
        }

        await RecordCatalogAsync(
            AuditActions.ServiceUpdated,
            nameof(Domain.Service),
            service.Id.ToString(),
            "Admin updated a service image.",
            cancellationToken);
        return ServiceResult<ServiceDto>.Success(ToServiceDto(service, service.Category.Name));
    }

    private async Task<ServiceResult<StoredCatalogImage>> StoreCatalogImageAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = DocumentFileValidator.ValidateCatalogImage(file);
        if (!validation.Succeeded)
        {
            return ServiceResult<StoredCatalogImage>.Validation("image", validation.Error!);
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        var storedFileName = await _storage.SaveAsync(
            buffer,
            validation.CanonicalExtension,
            cancellationToken);
        return ServiceResult<StoredCatalogImage>.Success(new StoredCatalogImage(
            storedFileName,
            validation.CanonicalContentType));
    }

    private static CategoryDto ToCategoryDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        HasImage = category.HasImage
    };

    private static ServiceDto ToServiceDto(Domain.Service service, string categoryName) => new()
    {
        Id = service.Id,
        Name = service.Name,
        Description = service.Description,
        CategoryId = service.CategoryId,
        CategoryName = categoryName,
        HasImage = service.HasImage
    };

    private sealed record StoredCatalogImage(string FileName, string ContentType);

    private static string CategoryDeleteBlockReason(IEnumerable<string> serviceNames)
    {
        var names = serviceNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        var noun = names.Count == 1 ? "service" : "services";
        var those = names.Count == 1 ? "that service" : "those services";
        return
            $"This category cannot be deleted because it still has {names.Count} {noun}: {FormatNameList(names)}. Move or delete {those} first.";
    }

    private static string? ServiceDeleteBlockReason(int providerCount, int bookingCount)
    {
        if (providerCount == 0 && bookingCount == 0)
        {
            return null;
        }

        var parts = new List<string>();
        if (providerCount > 0)
        {
            parts.Add(providerCount == 1
                ? "1 provider offers it"
                : $"{providerCount} providers offer it");
        }

        if (bookingCount > 0)
        {
            parts.Add(bookingCount == 1
                ? "1 booking uses it"
                : $"{bookingCount} bookings use it");
        }

        return $"This service cannot be deleted because {string.Join(" and ", parts)}.";
    }

    private static string FormatNameList(IReadOnlyList<string> names)
    {
        if (names.Count <= 1)
        {
            return names.Count == 0 ? "" : names[0];
        }

        if (names.Count == 2)
        {
            return $"{names[0]} and {names[1]}";
        }

        if (names.Count == 3)
        {
            return $"{names[0]}, {names[1]}, and {names[2]}";
        }

        return $"{names[0]}, {names[1]}, and {names.Count - 2} more";
    }

    private Task RecordCatalogAsync(
        string action,
        string entityType,
        string entityId,
        string message,
        CancellationToken cancellationToken) =>
        _audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Admin,
            Action = action,
            Outcome = AuditOutcomes.Success,
            EntityType = entityType,
            EntityId = entityId,
            Message = message
        }, cancellationToken);

    private static AuditLogListItemDto ToAuditListItem(AuditLog log, string summary) => new()
    {
        Id = log.Id,
        CreatedAt = log.CreatedAt,
        ActorUserId = log.ActorUserId,
        ActorEmail = log.ActorEmail,
        ActorRole = log.ActorRole,
        Category = log.Category,
        Action = log.Action,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        Outcome = log.Outcome,
        Message = log.Message,
        Summary = summary,
        IpAddress = log.IpAddress
    };

    private Task<bool> NameTakenAsync(
        string name,
        int? excludeId,
        CancellationToken cancellationToken) =>
        _db.Categories.AnyAsync(
            c => c.Name.ToLower() == name.ToLower() &&
                 (excludeId == null || c.Id != excludeId),
            cancellationToken);

    private Task<bool> ServiceNameTakenAsync(
        string name,
        int categoryId,
        int? excludeId,
        CancellationToken cancellationToken) =>
        _db.Services.AnyAsync(
            s => s.CategoryId == categoryId &&
                 s.Name.ToLower() == name.ToLower() &&
                 (excludeId == null || s.Id != excludeId),
            cancellationToken);
}
