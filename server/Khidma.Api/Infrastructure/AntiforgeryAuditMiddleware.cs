using Khidma.Api.Services.Audit;
using Microsoft.AspNetCore.Antiforgery;

namespace Khidma.Api.Infrastructure;

public sealed class AntiforgeryAuditMiddleware
{
    private const string RecordedKey = "__khidma.csrf.audit";
    private readonly RequestDelegate _next;

    public AntiforgeryAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IAuditService audit)
    {
        await _next(context);

        if (context.Response.StatusCode != StatusCodes.Status400BadRequest)
        {
            return;
        }

        if (!IsUnsafeMethod(context.Request.Method))
        {
            return;
        }

        if (await antiforgery.IsRequestValidAsync(context))
        {
            return;
        }

        if (!context.Items.TryAdd(RecordedKey, true))
        {
            return;
        }

        await audit.RecordAsync(new AuditEntry
        {
            Category = AuditCategories.Auth,
            Action = AuditActions.CsrfRejected,
            Outcome = AuditOutcomes.Denied,
            Message = "Antiforgery token validation failed."
        }, CancellationToken.None);
    }

    private static bool IsUnsafeMethod(string method) =>
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);
}
