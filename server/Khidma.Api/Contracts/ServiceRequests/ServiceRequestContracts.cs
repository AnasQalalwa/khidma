using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.ServiceRequests;

public sealed class CreateServiceRequestRequest
{
    [Required]
    public int ServiceId { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Title { get; set; } = default!;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Description { get; set; } = default!;

    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = default!;

    [Required]
    public DateTimeOffset PreferredDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BudgetMin { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BudgetMax { get; set; }
}

public sealed class UpdateServiceRequestRequest
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Title { get; set; } = default!;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Description { get; set; } = default!;

    [Required]
    public DateTimeOffset PreferredDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BudgetMin { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BudgetMax { get; set; }
}

public sealed class ServiceRequestSummaryDto
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset PreferredDate { get; init; }

    public decimal? BudgetMin { get; init; }

    public decimal? BudgetMax { get; init; }

    public required string Status { get; init; }

    public required int OfferCount { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class RequestDetailForCustomerDto
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset PreferredDate { get; init; }

    public decimal? BudgetMin { get; init; }

    public decimal? BudgetMax { get; init; }

    public required string Status { get; init; }

    public required int OfferCount { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required bool CanEdit { get; init; }

    public required bool CanCancel { get; init; }

    public int? BookingId { get; init; }

    public required IReadOnlyList<Contracts.Offers.OfferForCustomerDto> Offers { get; init; }
}

public sealed class RequestSummaryForProviderDto
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset PreferredDate { get; init; }

    public decimal? BudgetMin { get; init; }

    public decimal? BudgetMax { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class RequestDetailForProviderDto
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset PreferredDate { get; init; }

    public decimal? BudgetMin { get; init; }

    public decimal? BudgetMax { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required bool CanOffer { get; init; }

    public Contracts.Offers.OfferSnapshotDto? MyOffer { get; init; }
}

public sealed class RequestDetailForAdminDto
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required int ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string CategoryName { get; init; }

    public required string City { get; init; }

    public required DateTimeOffset PreferredDate { get; init; }

    public decimal? BudgetMin { get; init; }

    public decimal? BudgetMax { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerEmail { get; init; }

    public required int OfferCount { get; init; }

    public int? BookingId { get; init; }
}
