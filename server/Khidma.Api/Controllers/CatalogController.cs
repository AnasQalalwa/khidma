using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Services;
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

    [HttpGet("services/{serviceId:int}")]
    public async Task<IActionResult> GetService(
        int serviceId,
        CancellationToken cancellationToken)
    {
        return FromResult(await _catalog.GetServiceAsync(serviceId, cancellationToken));
    }

    [HttpGet("services/{serviceId:int}/providers")]
    public async Task<IActionResult> GetProvidersForService(
        int serviceId,
        [FromQuery] string? city,
        CancellationToken cancellationToken)
    {
        return FromResult(await _catalog.GetProvidersForServiceAsync(
            serviceId,
            city,
            cancellationToken));
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

    [HttpGet("categories/{categoryId:int}/image")]
    public Task<IActionResult> GetCategoryImage(
        int categoryId,
        CancellationToken cancellationToken) =>
        ImageAsync(_catalog.GetCategoryImageAsync(categoryId, cancellationToken));

    [HttpGet("services/{serviceId:int}/image")]
    public Task<IActionResult> GetServiceImage(
        int serviceId,
        CancellationToken cancellationToken) =>
        ImageAsync(_catalog.GetServiceImageAsync(serviceId, cancellationToken));

    private async Task<IActionResult> ImageAsync(
        Task<ServiceResult<Khidma.Api.Contracts.Verification.DocumentDownloadResult>> pending)
    {
        var result = await pending;
        if (!result.Succeeded)
        {
            return FromResult(result);
        }

        Response.Headers.CacheControl = "private, no-cache";
        return File(result.Value!.Content, result.Value.ContentType);
    }
}
