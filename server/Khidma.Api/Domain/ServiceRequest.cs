using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class ServiceRequest
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = default!;

    public int ServiceId { get; set; }

    public string Title { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string City { get; set; } = default!;

    public DateTimeOffset PreferredDate { get; set; }

    public decimal? BudgetMin { get; set; }

    public decimal? BudgetMax { get; set; }

    public ServiceRequestStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = default!;

    public ApplicationUser Customer { get; set; } = default!;

    public Service Service { get; set; } = default!;

    public ICollection<Offer> Offers { get; set; }
        = new List<Offer>();
}
