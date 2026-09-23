using Khidma.Api.Contracts.Account;
using Khidma.Api.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize]
[Route("api/account")]
public sealed class AccountController : ApiControllerBase
{
    private readonly IAccountService _account;

    public AccountController(IAccountService account)
    {
        _account = account;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        return FromResult(await _account.GetProfileAsync(RequireUserId(), cancellationToken));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _account.UpdateProfileAsync(
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _account.ChangePasswordAsync(
            RequireUserId(),
            request,
            cancellationToken);
        if (!result.Succeeded)
        {
            return FromResult(result);
        }

        return NoContent();
    }
}
