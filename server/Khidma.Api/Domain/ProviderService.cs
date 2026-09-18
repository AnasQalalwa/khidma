namespace Khidma.Api.Domain;

public class ProviderService
{
    public int Id { get; set; }

    public int ProviderProfileId { get; set; }

    public int ServiceId { get; set; }

    public ProviderProfile ProviderProfile { get; set; } = default!;

    public Service Service { get; set; } = default!;
}
