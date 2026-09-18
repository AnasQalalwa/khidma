using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Offers;

public sealed class SubmitOfferRequest
{
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Message { get; set; } = default!;

    [Required]
    public DateTimeOffset EstimatedDate { get; set; }
}

public sealed class OfferForCustomerDto
{
    public required int Id { get; init; }

    public required string ProviderId { get; init; }

    public required int ProviderProfileId { get; init; }

    public required string ProviderDisplayName { get; init; }

    public required decimal ProviderAverageRating { get; init; }

    public required int ProviderReviewCount { get; init; }

    public required decimal Price { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset EstimatedDate { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required bool CanAccept { get; init; }
}

public sealed class OfferSnapshotDto
{
    public required int Id { get; init; }

    public required decimal Price { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset EstimatedDate { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class OfferMineDto
{
    public required int Id { get; init; }

    public required int ServiceRequestId { get; init; }

    public required string RequestTitle { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required decimal Price { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset EstimatedDate { get; init; }

    public required string Status { get; init; }

    public required string RequestStatus { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
