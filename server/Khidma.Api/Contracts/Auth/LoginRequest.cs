using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}
