using Khidma.Api.Auth;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Services.Offers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize]
[Route("api/offers")]
public sealed class OffersController : ApiControllerBase
{
    private readonly IOfferService _offers;

    public OffersController(IOfferService offers)
    {
        _offers = offers;
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> Mine(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        return FromResult(await _offers.GetMineAsync(
            RequireUserId(),
            paging,
            cancellationToken));
    }

    [HttpPost("{id:int}/withdraw")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> Withdraw(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _offers.WithdrawAsync(
            id,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Accept(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _offers.AcceptAsync(
            id,
            RequireUserId(),
            cancellationToken));
    }
}
