using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Contracts.Schedule;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services;
using Khidma.Api.Services.Documents;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<ServiceDto>>> GetServicesByCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServiceDto>> GetServiceAsync(
        int serviceId,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<ServiceProviderDto>>> GetProvidersForServiceAsync(
        int serviceId,
        string? city,
        CancellationToken cancellationToken);

    Task<ServiceResult<DocumentDownloadResult>> GetCategoryImageAsync(
        int categoryId,
        CancellationToken cancellationToken);

    Task<ServiceResult<DocumentDownloadResult>> GetServiceImageAsync(
        int serviceId,
        CancellationToken cancellationToken);
}

public sealed class CatalogService : ICatalogService
{
    private readonly AppDbContext _db;
    private readonly IProviderDocumentStorage _storage;

    public CatalogService(AppDbContext db, IProviderDocumentStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                HasImage = c.ImageStoredFileName != null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Services
            .AsNoTracking()
            .OrderBy(s => s.Category.Name)
            .ThenBy(s => s.Name)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                HasImage = s.ImageStoredFileName != null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyList<ServiceDto>>> GetServicesByCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            return ServiceResult<IReadOnlyList<ServiceDto>>.NotFound("Category not found.");
        }

        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.CategoryId == categoryId)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                HasImage = s.ImageStoredFileName != null
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ServiceDto>>.Success(services);
    }

    public async Task<ServiceResult<ServiceDto>> GetServiceAsync(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .AsNoTracking()
            .Where(s => s.Id == serviceId)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                HasImage = s.ImageStoredFileName != null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return service is null
            ? ServiceResult<ServiceDto>.NotFound("Service not found.")
            : ServiceResult<ServiceDto>.Success(service);
    }

    public async Task<ServiceResult<IReadOnlyList<ServiceProviderDto>>> GetProvidersForServiceAsync(
        int serviceId,
        string? city,
        CancellationToken cancellationToken)
    {
        var serviceExists = await _db.Services
            .AsNoTracking()
            .AnyAsync(s => s.Id == serviceId, cancellationToken);
        if (!serviceExists)
        {
            return ServiceResult<IReadOnlyList<ServiceProviderDto>>.NotFound("Service not found.");
        }

        var query = _db.ProviderProfiles
            .AsNoTracking()
            .Where(p =>
                p.VerificationStatus == ProviderVerificationStatus.Approved &&
                !p.IsSuspended &&
                p.ProviderServices.Any(ps => ps.ServiceId == serviceId));

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalized = city.Trim().ToLower();
            query = query.Where(p => p.City.ToLower() == normalized);
        }

        var providers = await query
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ReviewCount)
            .ThenBy(p => p.User.FullName)
            .Select(p => new ServiceProviderDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                City = p.City,
                YearsOfExperience = p.YearsOfExperience,
                Bio = p.Bio,
                IsVerified = p.VerificationStatus == ProviderVerificationStatus.Approved,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                HasPhoto = p.PhotoStoredFileName != null,
                CompletedJobs = _db.Bookings.Count(b =>
                    b.ProviderId == p.UserId &&
                    b.Status == BookingStatus.Completed),
                WorkingHours = p.WorkingHours
                    .OrderBy(h => h.DayOfWeek)
                    .ThenBy(h => h.Hour)
                    .Select(h => new WorkingHourDto
                    {
                        DayOfWeek = h.DayOfWeek,
                        Hour = h.Hour
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ServiceProviderDto>>.Success(providers);
    }

    public Task<ServiceResult<DocumentDownloadResult>> GetCategoryImageAsync(
        int categoryId,
        CancellationToken cancellationToken) =>
        OpenImageAsync(
            _db.Categories.AsNoTracking().Where(c => c.Id == categoryId),
            categoryId,
            "category",
            cancellationToken);

    public Task<ServiceResult<DocumentDownloadResult>> GetServiceImageAsync(
        int serviceId,
        CancellationToken cancellationToken) =>
        OpenImageAsync(
            _db.Services.AsNoTracking().Where(s => s.Id == serviceId),
            serviceId,
            "service",
            cancellationToken);

    private async Task<ServiceResult<DocumentDownloadResult>> OpenImageAsync(
        IQueryable<Khidma.Api.Domain.Category> query,
        int id,
        string kind,
        CancellationToken cancellationToken)
    {
        var image = await query
            .Select(c => new { c.ImageStoredFileName, c.ImageContentType })
            .FirstOrDefaultAsync(cancellationToken);
        return await ReadImageAsync(image?.ImageStoredFileName, image?.ImageContentType, id, kind, cancellationToken);
    }

    private async Task<ServiceResult<DocumentDownloadResult>> OpenImageAsync(
        IQueryable<Khidma.Api.Domain.Service> query,
        int id,
        string kind,
        CancellationToken cancellationToken)
    {
        var image = await query
            .Select(s => new { s.ImageStoredFileName, s.ImageContentType })
            .FirstOrDefaultAsync(cancellationToken);
        return await ReadImageAsync(image?.ImageStoredFileName, image?.ImageContentType, id, kind, cancellationToken);
    }

    private async Task<ServiceResult<DocumentDownloadResult>> ReadImageAsync(
        string? storedFileName,
        string? contentType,
        int id,
        string kind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return ServiceResult<DocumentDownloadResult>.NotFound("Image not found.");
        }

        try
        {
            var content = await _storage.OpenReadAsync(storedFileName, cancellationToken);
            return ServiceResult<DocumentDownloadResult>.Success(new DocumentDownloadResult
            {
                Content = content,
                ContentType = contentType ?? "image/jpeg",
                OriginalFileName = $"{kind}-{id}{Path.GetExtension(storedFileName)}"
            });
        }
        catch (FileNotFoundException)
        {
            return ServiceResult<DocumentDownloadResult>.NotFound("Image not found.");
        }
    }
}
