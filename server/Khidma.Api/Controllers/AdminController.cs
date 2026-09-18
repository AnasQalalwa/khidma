using Khidma.Api.Auth;
using Khidma.Api.Contracts.Admin;
using Khidma.Api.Contracts.Audit;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Verification;
using Khidma.Api.Services.Admin;
using Khidma.Api.Services.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly IAdminService _admin;
    private readonly IProviderVerificationService _verification;

    public AdminController(
        IAdminService admin,
        IProviderVerificationService verification)
    {
        _admin = admin;
        _verification = verification;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetStatsAsync(cancellationToken));
    }

    [HttpGet("attention")]
    public async Task<IActionResult> Attention(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetAttentionAsync(cancellationToken));
    }

    [HttpGet("providers")]
    public async Task<IActionResult> Providers(
        [FromQuery] AdminProviderQuery query,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.GetProvidersAsync(query, cancellationToken));
    }

    [HttpGet("verifications")]
    public async Task<IActionResult> Verifications(
        [FromQuery] AdminVerificationQuery query,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.ListVerificationsAsync(query, cancellationToken));
    }

    [HttpGet("providers/{id:int}/verification")]
    public async Task<IActionResult> ProviderVerification(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.GetAdminDetailAsync(id, cancellationToken));
    }

    [HttpGet("verification-documents/{documentId:int}/download")]
    public async Task<IActionResult> DownloadDocument(
        int documentId,
        CancellationToken cancellationToken)
    {
        return FileFromResult(
            await _verification.DownloadAsync(
                documentId,
                RequireUserId(),
                isAdmin: true,
                cancellationToken));
    }

    [HttpPost("verification-documents/{documentId:int}/review")]
    public async Task<IActionResult> ReviewDocument(
        int documentId,
        [FromBody] ReviewVerificationDocumentRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.ReviewDocumentAsync(
            documentId,
            request,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("providers/{id:int}/verification")]
    public async Task<IActionResult> DecideVerification(
        int id,
        [FromBody] ProviderVerificationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.DecideProviderAsync(
            id,
            request,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("providers/{id:int}/suspension")]
    public async Task<IActionResult> SetSuspension(
        int id,
        [FromBody] SetProviderSuspensionRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _verification.SetSuspensionAsync(
            id,
            request,
            RequireUserId(),
            cancellationToken));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] AdminUserQuery query,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.GetUsersAsync(query, cancellationToken));
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> UserDetail(
        string userId,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.GetUserAsync(userId, cancellationToken));
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.GetAuditLogsAsync(query, cancellationToken));
    }

    [HttpGet("audit-logs/summary")]
    public async Task<IActionResult> AuditSummary(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetAuditSummaryAsync(cancellationToken));
    }

    [HttpGet("audit-logs/{id:long}")]
    public async Task<IActionResult> AuditLogDetail(
        long id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _admin.GetAuditLogAsync(id, cancellationToken));
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

    private IActionResult FileFromResult(
        Services.ServiceResult<DocumentDownloadResult> result)
    {
        if (!result.Succeeded)
        {
            return FromResult(result);
        }

        return File(
            result.Value!.Content,
            result.Value.ContentType,
            result.Value.OriginalFileName);
    }
}
