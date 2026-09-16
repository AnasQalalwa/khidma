using System.Security.Claims;

namespace Khidma.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;

    public static string? GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role);
}
