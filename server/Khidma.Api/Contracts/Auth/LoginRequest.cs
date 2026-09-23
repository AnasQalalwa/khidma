using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email like you@example.com.")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Enter your password.")]
    public string Password { get; set; } = default!;
}
