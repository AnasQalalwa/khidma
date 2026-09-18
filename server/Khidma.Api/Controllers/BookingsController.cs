using Khidma.Api.Auth;
using Khidma.Api.Contracts.Bookings;
using Khidma.Api.Contracts.Common;
using Khidma.Api.Contracts.Reviews;
using Khidma.Api.Services.Bookings;
using Khidma.Api.Services.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize]
[Route("api/bookings")]
public sealed class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookings;
    private readonly IReviewService _reviews;

    public BookingsController(IBookingService bookings, IReviewService reviews)
    {
        _bookings = bookings;
        _reviews = reviews;
    }

    [HttpGet("mine")]
    [Authorize(Roles = $"{AppRoles.Customer},{AppRoles.Provider}")]
    public async Task<IActionResult> Mine(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        return FromResult(await _bookings.GetMineAsync(
            RequireUserId(),
            CallerRole(),
            paging,
            cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _bookings.GetByIdAsync(
            id,
            RequireUserId(),
            CallerIsAdmin(),
            cancellationToken));
    }

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> Start(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _bookings.StartAsync(
            id,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> Complete(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _bookings.CompleteAsync(
            id,
            RequireUserId(),
            cancellationToken));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = $"{AppRoles.Customer},{AppRoles.Provider}")]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _bookings.CancelAsync(
            id,
            RequireUserId(),
            request.Reason,
            cancellationToken));
    }

    [HttpPost("{id:int}/review")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> CreateReview(
        int id,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(await _reviews.CreateAsync(
            id,
            RequireUserId(),
            request,
            cancellationToken));
    }

    [HttpGet("{id:int}/review")]
    public async Task<IActionResult> GetReview(
        int id,
        CancellationToken cancellationToken)
    {
        return FromResult(await _reviews.GetForBookingAsync(
            id,
            RequireUserId(),
            CallerIsAdmin(),
            cancellationToken));
    }
}
