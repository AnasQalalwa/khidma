using Khidma.Api.Auth;
using Khidma.Api.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("customer")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Customer(CancellationToken cancellationToken)
    {
        return FromResult(await _dashboard.GetCustomerAsync(
            RequireUserId(),
            cancellationToken));
    }

    [HttpGet("provider")]
    [Authorize(Roles = AppRoles.Provider)]
    public async Task<IActionResult> Provider(CancellationToken cancellationToken)
    {
        return FromResult(await _dashboard.GetProviderAsync(
            RequireUserId(),
            cancellationToken));
    }
}
