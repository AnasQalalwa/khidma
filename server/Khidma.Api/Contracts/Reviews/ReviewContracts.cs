using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Reviews;

public sealed class CreateReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public sealed class ReviewDto
{
    public required int Id { get; init; }

    public required int Rating { get; init; }

    public string? Comment { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string CustomerDisplayName { get; init; }
}

public sealed class PublicReviewDto
{
    public required string ReviewerFirstName { get; init; }

    public required int Rating { get; init; }

    public string? Comment { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
