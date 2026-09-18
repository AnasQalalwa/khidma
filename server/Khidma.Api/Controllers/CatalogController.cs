using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[AllowAnonymous]
public sealed class CatalogController : ApiControllerBase
{
    private readonly ICatalogService _catalog;

    public CatalogController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        return Ok(await _catalog.GetCategoriesAsync(cancellationToken));
    }

    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServices(
        CancellationToken cancellationToken)
    {
        return Ok(await _catalog.GetServicesAsync(cancellationToken));
    }

    [HttpGet("categories/{categoryId:int}/services")]
    public async Task<IActionResult> GetServicesByCategory(
        int categoryId,
        CancellationToken cancellationToken)
    {
        return FromResult(await _catalog.GetServicesByCategoryAsync(
            categoryId,
            cancellationToken));
    }
}
