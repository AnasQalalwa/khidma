using Khidma.Api.Auth;
using Khidma.Api.Contracts.Providers;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Infrastructure;
using Khidma.Api.Services.Providers;
using Khidma.Api.Services.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Route("api/providers")]
public sealed class ProvidersController : ApiControllerBase
{
    private readonly IProviderProfileService _providers;
    private readonly IProviderVerificationService _verification;

    public ProvidersController(
        IProviderProfileService providers,
        IProviderVerificationService verification)
    {
        _providers = providers;
        _verification = verification;
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

    [HttpGet("me/verification")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> GetMyVerification(CancellationToken cancellationToken)
    {
        return FromResult(await _verification.GetMineAsync(
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("me/verification-documents")]
    [Authorize(Roles = AppRoles.Provider)]
    [RequestSizeLimit(DocumentFileValidator.MaxFileSizeBytes + 256 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentFileValidator.MaxFileSizeBytes + 256 * 1024)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] string documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.UploadAsync(
            RequireUserId(),
            documentType,
            file,
            cancellationToken));
    }

    [HttpDelete("me/verification-documents/{documentId:int}")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> DeleteDocument(
        int documentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.DeleteMineAsync(
            RequireUserId(),
            documentId,
            cancellationToken));
    }

    [HttpGet("me/verification-documents/{documentId:int}/download")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> DownloadMine(
        int documentId,
        CancellationToken cancellationToken)
    {
        var result = await _verification.DownloadAsync(
            documentId,
            RequireUserId(),
            isAdmin: false,
            cancellationToken);

        if (!result.Succeeded)
        {
            return FromResult(result);
        }

        return File(
            result.Value!.Content,
            result.Value.ContentType,
            result.Value.OriginalFileName);
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
