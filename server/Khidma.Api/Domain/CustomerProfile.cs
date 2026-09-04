namespace Khidma.Api.Domain;

public class CustomerProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    public string City { get; set; } = default!;

    public string? DefaultContact { get; set; }

    public ApplicationUser User { get; set; } = default!;
}
