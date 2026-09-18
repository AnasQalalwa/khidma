using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Data;
using Khidma.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<ServiceDto>>> GetServicesByCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken);
}

public sealed class CatalogService : ICatalogService
{
    private readonly AppDbContext _db;

    public CatalogService(AppDbContext db)
    {
        _db = db;
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
                Name = c.Name
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
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name
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
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ServiceDto>>.Success(services);
    }
}
