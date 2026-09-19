using Khidma.Api.Auth;
using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Audit;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Admin;

public sealed partial class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public AdminService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
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
            OpenRequests = await _db.ServiceRequests
                .CountAsync(r => r.Status == ServiceRequestStatus.Open, cancellationToken),
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
            dto.RequestCount = await _db.ServiceRequests.CountAsync(
                r => r.CustomerId == user.Id,
                cancellationToken);
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
            dto.OfferCount = await _db.Offers.CountAsync(
                o => o.ProviderId == user.Id,
                cancellationToken);
            dto.ActiveBookingCount = await _db.Bookings.CountAsync(
                b => b.ProviderId == user.Id &&
                     (b.Status == BookingStatus.Scheduled ||
                      b.Status == BookingStatus.InProgress),
                cancellationToken);
            dto.CompletedBookingCount = await _db.Bookings.CountAsync(
                b => b.ProviderId == user.Id && b.Status == BookingStatus.Completed,
                cancellationToken);
            dto.BookingCount = dto.ActiveBookingCount + dto.CompletedBookingCount +
                await _db.Bookings.CountAsync(
                    b => b.ProviderId == user.Id && b.Status == BookingStatus.Cancelled,
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

        var category = new Category { Name = name };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.CategoryCreated,
            nameof(Category),
            category.Id.ToString(),
            "Admin created a category.",
            cancellationToken);
        return ServiceResult<CategoryDto>.Success(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        });
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
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.CategoryUpdated,
            nameof(Category),
            category.Id.ToString(),
            "Admin updated a category.",
            cancellationToken);
        return ServiceResult<CategoryDto>.Success(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        });
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
                "This category cannot be deleted while it still has services.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
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

        return ServiceResult<ServiceDto>.Success(new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            CategoryId = service.CategoryId,
            CategoryName = category.Name
        });
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
        service.CategoryId = request.CategoryId;
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.ServiceUpdated,
            nameof(Domain.Service),
            service.Id.ToString(),
            "Admin updated a service.",
            cancellationToken);

        return ServiceResult<ServiceDto>.Success(new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            CategoryId = service.CategoryId,
            CategoryName = category.Name
        });
    }

    public async Task<ServiceResult<bool>> DeleteServiceAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .Include(s => s.ProviderServices)
            .Include(s => s.ServiceRequests)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service is null)
        {
            return ServiceResult<bool>.NotFound("Service not found.");
        }

        if (service.ProviderServices.Count > 0 || service.ServiceRequests.Count > 0)
        {
            return ServiceResult<bool>.Conflict(
                "This service cannot be deleted because it is used by providers or requests.");
        }

        _db.Services.Remove(service);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordCatalogAsync(
            AuditActions.ServiceDeleted,
            nameof(Domain.Service),
            id.ToString(),
            "Admin deleted a service.",
            cancellationToken);
        return ServiceResult<bool>.Success(true);
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
