using Microsoft.AspNetCore.Identity;

namespace Khidma.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public CustomerProfile? CustomerProfile { get; set; }

    public ProviderProfile? ProviderProfile { get; set; }
}
