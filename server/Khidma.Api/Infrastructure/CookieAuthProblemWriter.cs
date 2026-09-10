using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Khidma.Api.Infrastructure;

public static class CookieAuthProblemWriter
{
    public static Task WriteAsync(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode,
        string title)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers.Location = string.Empty;

        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title
        });
    }
}
