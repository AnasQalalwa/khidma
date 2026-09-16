using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Admin;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext db, ILogger<AdminService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        return new AdminStatsDto
        {
            Customers = await _db.CustomerProfiles.CountAsync(cancellationToken),
            Providers = await _db.ProviderProfiles.CountAsync(cancellationToken),
            PendingProviders = await _db.ProviderProfiles
                .CountAsync(p => !p.IsApproved, cancellationToken),
            Categories = await _db.Categories.CountAsync(cancellationToken),
            Services = await _db.Services.CountAsync(cancellationToken),
            OpenRequests = await _db.ServiceRequests
                .CountAsync(r => r.Status == ServiceRequestStatus.Open, cancellationToken),
            ActiveBookings = await _db.Bookings.CountAsync(
                b => b.Status == BookingStatus.Scheduled ||
                     b.Status == BookingStatus.InProgress,
                cancellationToken),
            CompletedBookings = await _db.Bookings
                .CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken)
        };
    }

    public async Task<ServiceResult<PagedResult<AdminProviderListItemDto>>> GetProvidersAsync(
        PageQuery paging,
        bool? approved,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = paging.Normalize();
        var query = _db.ProviderProfiles.AsNoTracking();

        if (approved is not null)
        {
            query = query.Where(p => p.IsApproved == approved);
        }

        var pageResult = await query
            .OrderBy(p => p.User.FullName)
            .Select(p => new AdminProviderListItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FullName = p.User.FullName,
                Email = p.User.Email ?? string.Empty,
                City = p.City,
                IsApproved = p.IsApproved,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                Services = p.ProviderServices
                    .OrderBy(ps => ps.Service.Name)
                    .Select(ps => ps.Service.Name)
                    .ToList()
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        return ServiceResult<PagedResult<AdminProviderListItemDto>>.Success(pageResult);
    }

    public async Task<ServiceResult<AdminProviderListItemDto>> SetApprovalAsync(
        int providerProfileId,
        bool isApproved,
        string adminUserId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
            .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<AdminProviderListItemDto>.NotFound("Provider not found.");
        }

        profile.IsApproved = isApproved;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {UserId} set provider {ProviderProfileId} approval to {IsApproved}",
            adminUserId,
            profile.Id,
            isApproved);

        return ServiceResult<AdminProviderListItemDto>.Success(new AdminProviderListItemDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.User.FullName,
            Email = profile.User.Email ?? string.Empty,
            City = profile.City,
            IsApproved = profile.IsApproved,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            Services = profile.ProviderServices
                .OrderBy(ps => ps.Service.Name)
                .Select(ps => ps.Service.Name)
                .ToList()
        });
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
        return ServiceResult<bool>.Success(true);
    }

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
