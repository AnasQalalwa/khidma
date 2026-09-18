using System.Text.Json;
using Khidma.Api.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Http;

namespace Khidma.Api.Services.Audit;

public sealed class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> ForbiddenDetailKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordhash",
        "token",
        "csrftoken",
        "xsrf",
        "cookie",
        "securitystamp",
        "concurrencystamp",
        "connectionstring",
        "secret",
        "bytes",
        "content",
        "filebytes"
    };

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var http = _httpContextAccessor.HttpContext;
            var user = http?.User;

            var audit = new AuditLog
            {
                CreatedAt = DateTimeOffset.UtcNow,
                ActorUserId = FirstNonEmpty(entry.ActorUserId, user?.GetUserId()),
                ActorEmail = Truncate(
                    FirstNonEmpty(entry.ActorEmail, user?.GetEmail()),
                    256),
                ActorRole = Truncate(
                    FirstNonEmpty(entry.ActorRole, user?.GetRole()),
                    50),
                Category = Truncate(entry.Category, 50) ?? "Unknown",
                Action = Truncate(entry.Action, 100) ?? "Unknown",
                EntityType = Truncate(entry.EntityType, 100),
                EntityId = Truncate(entry.EntityId, 100),
                Outcome = Truncate(entry.Outcome, 20) ?? AuditOutcomes.Success,
                Message = Truncate(entry.Message, 1000),
                DetailsJson = SerializeDetails(entry.Details),
                IpAddress = Truncate(GetIp(http), 64),
                UserAgent = Truncate(http?.Request.Headers.UserAgent.ToString(), 512),
                CorrelationId = Truncate(http?.TraceIdentifier, 64)
            };

            _db.AuditLogs.Add(audit);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist audit event {Action}",
                entry.Action);
        }
    }

    private static string? SerializeDetails(IReadOnlyDictionary<string, object?>? details)
    {
        if (details is null || details.Count == 0)
        {
            return null;
        }

        var safe = details
            .Where(pair => !ForbiddenDetailKeys.Contains(pair.Key) && pair.Value is not byte[])
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return safe.Count == 0 ? null : JsonSerializer.Serialize(safe, JsonOptions);
    }

    private static string? GetIp(HttpContext? http)
    {
        if (http is null)
        {
            return null;
        }

        var forwarded = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
