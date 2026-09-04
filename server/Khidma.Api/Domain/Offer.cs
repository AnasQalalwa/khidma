using Khidma.Api.Domain.Enums;

namespace Khidma.Api.Domain;

public class Offer
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    public string ProviderId { get; set; } = default!;

    public decimal Price { get; set; }

    public string Message { get; set; } = default!;

    public DateTimeOffset EstimatedDate { get; set; }

    public OfferStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = default!;

    public ApplicationUser Provider { get; set; } = default!;

    public Booking? Booking { get; set; }
}
