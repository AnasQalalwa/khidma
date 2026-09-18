namespace Khidma.Api.Contracts.Audit;

public sealed class AuditLogListItemDto
{
    public required long Id { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorEmail { get; init; }

    public string? ActorRole { get; init; }

    public required string Category { get; init; }

    public required string Action { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public required string Outcome { get; init; }

    public string? Message { get; init; }

    public string? IpAddress { get; init; }
}

public sealed class AuditLogDetailDto
{
    public required long Id { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorEmail { get; init; }

    public string? ActorRole { get; init; }

    public required string Category { get; init; }

    public required string Action { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public required string Outcome { get; init; }

    public string? Message { get; init; }

    public string? DetailsJson { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }
}

public sealed class AuditLogQuery : Common.PageQuery
{
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public string? ActorUserId { get; set; }

    public string? ActorEmail { get; set; }

    public string? Category { get; set; }

    public string? Action { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? Outcome { get; set; }

    public string? Search { get; set; }
}

public sealed class AuditSummaryDto
{
    public required int EventsToday { get; init; }

    public required int DeniedActions { get; init; }

    public required int AdminActions { get; init; }

    public required int ProviderVerificationEvents { get; init; }
}
