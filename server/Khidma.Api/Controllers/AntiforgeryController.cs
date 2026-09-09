using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Controllers;

[ApiController]
[Route("api/antiforgery")]
public sealed class AntiforgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("token")]
    [AllowAnonymous]
    public IActionResult GetToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            });

        return NoContent();
    }
}
