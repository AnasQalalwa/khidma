using Khidma.Api.Contracts.Audit;
using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Admin;

public sealed class AdminStatsDto
{
    public required int TotalUsers { get; init; }

    public required int Customers { get; init; }

    public required int Providers { get; init; }

    public required int PendingProviders { get; init; }

    public required int PendingVerification { get; init; }

    public required int ApprovedProviders { get; init; }

    public required int SuspendedProviders { get; init; }

    public required int PendingDocuments { get; init; }

    public required int RejectedDocuments { get; init; }

    public required int Categories { get; init; }

    public required int Services { get; init; }

    public required int OpenRequests { get; init; }

    public required int ActiveBookings { get; init; }

    public required int CompletedBookings { get; init; }

    public required int AuditEventsLast24h { get; init; }
}

public sealed class AdminAttentionDto
{
    public required IReadOnlyList<AdminAttentionItemDto> Items { get; init; }
}

public sealed class AdminAttentionItemDto
{
    public required string Kind { get; init; }

    public required string Title { get; init; }

    public required string Detail { get; init; }

    public required string Href { get; init; }
}

public sealed class AdminProviderListItemDto
{
    public required int Id { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string City { get; init; }

    public required string VerificationStatus { get; init; }

    public required bool IsSuspended { get; init; }

    public string? SuspensionReason { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required int DocumentCount { get; init; }

    public required int ApprovedDocumentCount { get; init; }

    public required IReadOnlyList<string> Services { get; init; }
}

public sealed class AdminProviderQuery : Common.PageQuery
{
    public string? VerificationStatus { get; set; }

    public bool? Suspended { get; set; }

    public string? Search { get; set; }
}

public sealed class SaveCategoryRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = default!;
}

public sealed class SaveServiceRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [Required]
    public int CategoryId { get; set; }
}

public sealed class AdminUserQuery : Common.PageQuery
{
    public string? Search { get; set; }

    public string? Role { get; set; }

    public string? ProviderVerificationStatus { get; set; }

    public bool? Suspended { get; set; }
}

public sealed class AdminUserListItemDto
{
    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string Role { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    public int? ProviderProfileId { get; init; }

    public string? VerificationStatus { get; init; }

    public bool? IsSuspended { get; init; }

    public decimal? AverageRating { get; init; }

    public int? ReviewCount { get; init; }

    public string? City { get; init; }
}

public sealed class AdminUserDetailDto
{
    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string Role { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    public string? City { get; set; }

    public int RequestCount { get; set; }

    public int BookingCount { get; set; }

    public int ReviewCount { get; set; }

    public int? ProviderProfileId { get; set; }

    public string? VerificationStatus { get; set; }

    public bool? IsSuspended { get; set; }

    public string? SuspensionReason { get; set; }

    public decimal? AverageRating { get; set; }

    public int OfferCount { get; set; }

    public int ActiveBookingCount { get; set; }

    public int CompletedBookingCount { get; set; }

    public IReadOnlyList<string> Services { get; set; } = [];

    public required IReadOnlyList<AuditLogListItemDto> RecentAuditEvents { get; init; }
}
