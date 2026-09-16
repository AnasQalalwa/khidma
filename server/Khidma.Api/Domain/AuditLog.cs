namespace Khidma.Api.Domain;

public class AuditLog
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? ActorUserId { get; set; }

    public string? ActorEmail { get; set; }

    public string? ActorRole { get; set; }

    public string Category { get; set; } = default!;

    public string Action { get; set; } = default!;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string Outcome { get; set; } = default!;

    public string? Message { get; set; }

    public string? DetailsJson { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }
}
