namespace Khidma.Api.Domain;

public class ProviderProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    public string City { get; set; } = default!;

    public int YearsOfExperience { get; set; }

    public string? Bio { get; set; }

    public bool IsApproved { get; set; }

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public ApplicationUser User { get; set; } = default!;

    public ICollection<ProviderService> ProviderServices { get; set; }
        = new List<ProviderService>();
}
