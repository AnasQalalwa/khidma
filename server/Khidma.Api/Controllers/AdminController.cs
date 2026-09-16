using Khidma.Api.Auth;
using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetStatsAsync(cancellationToken));
    }

    [HttpGet("providers")]
    public async Task<IActionResult> Providers(
        [FromQuery] PageQuery paging,
        [FromQuery] bool? approved,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.GetProvidersAsync(
            paging,
            approved,
            cancellationToken));
    }

    [HttpPost("providers/{id:int}/approval")]
    public async Task<IActionResult> SetApproval(
        int id,
        [FromBody] SetProviderApprovalRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.SetApprovalAsync(
            id,
            request.IsApproved,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.CreateCategoryAsync(request, cancellationToken));
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.UpdateCategoryAsync(id, request, cancellationToken));
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.DeleteCategoryAsync(id, cancellationToken));
    }

    [HttpPost("services")]
    public async Task<IActionResult> CreateService(
        [FromBody] SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.CreateServiceAsync(request, cancellationToken));
    }

    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> UpdateService(
        int id,
        [FromBody] SaveServiceRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.UpdateServiceAsync(id, request, cancellationToken));
    }

    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.DeleteServiceAsync(id, cancellationToken));
    }
}
