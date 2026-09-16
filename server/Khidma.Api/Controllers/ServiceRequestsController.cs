using Khidma.Api.Auth;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Offers;
using Khidma.Api.Contracts.ServiceRequests;
using Khidma.Api.Services.Offers;
using Khidma.Api.Services.ServiceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize]
[Route("api/service-requests")]
public sealed class ServiceRequestsController : ApiControllerBase
{
    private readonly IServiceRequestService _requests;
    private readonly IOfferService _offers;

    public ServiceRequestsController(
        IServiceRequestService requests,
        IOfferService offers)
    {
        _requests = requests;
        _offers = offers;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _requests.CreateAsync(
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Mine(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        return FromResult(await _requests.GetMineAsync(
            RequireUserId(),
            paging,
            cancellationToken));
    }

    [HttpGet("available")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> Available(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        return FromResult(await _requests.GetAvailableAsync(
            RequireUserId(),
            paging,
            cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var role = User.IsInRole(AppRoles.Admin)
            ? AppRoles.Admin
            : User.IsInRole(AppRoles.Provider)
                ? AppRoles.Provider
                : AppRoles.Customer;

        return FromResult(await _requests.GetByIdAsync(
            id,
            RequireUserId(),
            role,
            cancellationToken));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _requests.UpdateAsync(
            id,
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _requests.CancelAsync(
            id,
            RequireUserId(),
            cancellationToken));
    }

    [HttpGet("{id:int}/offers")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> ListOffers(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _offers.ListForRequestAsync(
            id,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("{id:int}/offers")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> SubmitOffer(
        int id,
        [FromBody] SubmitOfferRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _offers.SubmitAsync(
            id,
            RequireUserId(),
            request,
            cancellationToken));
    }
}
