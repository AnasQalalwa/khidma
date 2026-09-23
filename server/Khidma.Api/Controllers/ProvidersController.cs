using Khidma.Api.Auth;
using Khidma.Api.Contracts.Providers;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Infrastructure;
using Khidma.Api.Contracts.Schedule;
using Khidma.Api.Services.Providers;
using Khidma.Api.Services.Schedule;
using Khidma.Api.Services.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Route("api/providers")]
public sealed class ProvidersController : ApiControllerBase
{
    private readonly IProviderProfileService _providers;
    private readonly IProviderVerificationService _verification;
    private readonly IProviderScheduleService _schedule;

    public ProvidersController(
        IProviderProfileService providers,
        IProviderVerificationService verification,
        IProviderScheduleService schedule)
    {
        _providers = providers;
        _verification = verification;
        _schedule = schedule;
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

    [HttpPost("me/location-changes")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> RequestLocationChange(
        [FromBody] RequestLocationChangeRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _providers.RequestLocationChangeAsync(
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpPost("me/service-changes")]
    [Authorize(Roles = AppRoles.Provider)]
    [RequestSizeLimit(DocumentFileValidator.MaxFileSizeBytes + 256 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentFileValidator.MaxFileSizeBytes + 256 * 1024)]
    public async Task<IActionResult> RequestServiceAddition(
        [FromForm] int serviceId,
        [FromForm] string documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return FromResult(await _providers.RequestServiceAdditionAsync(
            RequireUserId(),
            serviceId,
            documentType,
            file,
            cancellationToken));
    }

    [HttpPost("me/photo")]
    [Authorize(Roles = AppRoles.Provider)]
    [RequestSizeLimit(DocumentFileValidator.MaxImageSizeBytes + 256 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentFileValidator.MaxImageSizeBytes + 256 * 1024)]
    public async Task<IActionResult> UploadPhoto(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return FromResult(await _providers.UploadPhotoAsync(
            RequireUserId(),
            file,
            cancellationToken));
    }

    [HttpDelete("me/photo")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> DeletePhoto(CancellationToken cancellationToken)
    {
        return FromResult(await _providers.DeletePhotoAsync(
            RequireUserId(),
            cancellationToken));
    }

    [HttpGet("{id:int}/photo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPhoto(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _providers.GetPhotoAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return FromResult(result);
        }

        Response.Headers.CacheControl = "private, no-cache";
        return File(result.Value!.Content, result.Value.ContentType);
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
        [FromForm] int? serviceId,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.UploadAsync(
            RequireUserId(),
            documentType,
            file,
            serviceId,
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

    [HttpGet("me/working-hours")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> GetWorkingHours(CancellationToken cancellationToken)
    {
        return FromResult(await _schedule.GetWorkingHoursAsync(RequireUserId(), cancellationToken));
    }

    [HttpPut("me/working-hours")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> ReplaceWorkingHours(
        [FromBody] UpdateWorkingHoursRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _schedule.ReplaceWorkingHoursAsync(
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpGet("me/schedule")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        return FromResult(await _schedule.GetMyScheduleAsync(
            RequireUserId(),
            from,
            to,
            cancellationToken));
    }

    [HttpGet("{id:int}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(
        int id,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        return FromResult(await _schedule.GetAvailabilityAsync(id, from, to, cancellationToken));
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
