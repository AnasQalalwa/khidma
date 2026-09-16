using Khidma.Api.Auth;
using Khidma.Api.Contracts.Providers;
using Khidma.Api.Services.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Route("api/providers")]
public sealed class ProvidersController : ApiControllerBase
{
    private readonly IProviderProfileService _providers;

    public ProvidersController(IProviderProfileService providers)
    {
        _providers = providers;
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        return FromResult(await _providers.GetMeAsync(
            RequireUserId(),
            cancellationToken));
    }

    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProviderProfileRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _providers.UpdateMeAsync(
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpPut("me/services")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> ReplaceServices(
        [FromBody] ReplaceProviderServicesRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _providers.ReplaceServicesAsync(
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _providers.GetPublicAsync(id, cancellationToken));
    }
}
