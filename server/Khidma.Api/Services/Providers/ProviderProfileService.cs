using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Providers;
using Khidma.Api.Contracts.Reviews;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Services.ServiceRequests;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Providers;

public sealed class ProviderProfileService : IProviderProfileService
{
    private readonly AppDbContext _db;

    public ProviderProfileService(AppDbContext db)
    {
        _db = db;
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
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return ServiceResult<ProviderMeDto>.NotFound("Provider profile not found.");
        }

        profile.City = request.City.Trim();
        profile.YearsOfExperience = request.YearsOfExperience;
        profile.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

        await _db.SaveChangesAsync(cancellationToken);
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
        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<ServiceResult<PublicProviderDto>> GetPublicAsync(
        int providerProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
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
            IsApproved = profile.IsApproved,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            Services = profile.ProviderServices
                .OrderBy(ps => ps.Service.Category.Name)
                .ThenBy(ps => ps.Service.Name)
                .Select(ps => new ServiceDto
                {
                    Id = ps.Service.Id,
                    Name = ps.Service.Name,
                    CategoryId = ps.Service.CategoryId,
                    CategoryName = ps.Service.Category.Name
                })
                .ToList(),
            RecentReviews = reviews
                .Select(r => new PublicReviewDto
                {
                    ReviewerFirstName = RequestValidation.FirstName(r.ReviewerFirstName),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToList()
        });
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
            City = profile.City,
            YearsOfExperience = profile.YearsOfExperience,
            Bio = profile.Bio,
            IsApproved = profile.IsApproved,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            Services = profile.ProviderServices
                .OrderBy(ps => ps.Service.Category.Name)
                .ThenBy(ps => ps.Service.Name)
                .Select(ps => new ServiceDto
                {
                    Id = ps.Service.Id,
                    Name = ps.Service.Name,
                    CategoryId = ps.Service.CategoryId,
                    CategoryName = ps.Service.Category.Name
                })
                .ToList()
        };
    }
}
