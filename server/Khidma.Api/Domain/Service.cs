namespace Khidma.Api.Domain;

public class Service
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = default!;

    public ICollection<ProviderService> ProviderServices { get; set; }
        = new List<ProviderService>();

    public ICollection<ServiceRequest> ServiceRequests { get; set; }
        = new List<ServiceRequest>();
}
