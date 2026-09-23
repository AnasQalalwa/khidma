namespace Khidma.Api.Domain;

public class Service
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = "";

    public string? ImageStoredFileName { get; set; }

    public string? ImageContentType { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageStoredFileName);

    public int CategoryId { get; set; }

    public Category Category { get; set; } = default!;

    public ICollection<ProviderService> ProviderServices { get; set; }
        = new List<ProviderService>();

    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();
}
