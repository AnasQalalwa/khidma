using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[ApiController]
[Route("api/antiforgery")]
public sealed class AntiforgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;
    private readonly IHostEnvironment _environment;

    public AntiforgeryController(IAntiforgery antiforgery, IHostEnvironment environment)
    {
        _antiforgery = antiforgery;
        _environment = environment;
    }

    [HttpGet("token")]
    [AllowAnonymous]
    public IActionResult GetToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        var secure = Request.IsHttps ||
            (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"));

        Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            });

        return NoContent();
    }
}
