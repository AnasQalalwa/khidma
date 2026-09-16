using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Admin;

public sealed class AdminStatsDto
{
    public required int Customers { get; init; }

    public required int Providers { get; init; }

    public required int PendingProviders { get; init; }

    public required int Categories { get; init; }

    public required int Services { get; init; }

    public required int OpenRequests { get; init; }

    public required int ActiveBookings { get; init; }

    public required int CompletedBookings { get; init; }
}

public sealed class AdminProviderListItemDto
{
    public required int Id { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string City { get; init; }

    public required bool IsApproved { get; init; }

    public required decimal AverageRating { get; init; }

    public required int ReviewCount { get; init; }

    public required IReadOnlyList<string> Services { get; init; }
}

public sealed class SetProviderApprovalRequest
{
    public bool IsApproved { get; set; }
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
