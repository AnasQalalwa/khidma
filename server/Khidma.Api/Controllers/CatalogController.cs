using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[AllowAnonymous]
public sealed class CatalogController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServices(
        CancellationToken cancellationToken)
    {
        var services = await _db.Services
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

        return Ok(services);
    }

    [HttpGet("categories/{categoryId:int}/services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServicesByCategory(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Category not found."
            });
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

        return Ok(services);
    }
}
