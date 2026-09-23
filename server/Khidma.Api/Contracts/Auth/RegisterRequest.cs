using System.ComponentModel.DataAnnotations;
using Khidma.Api.Auth;

namespace Khidma.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = default!;

    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email like you@example.com.")]
    [RegularExpression(
        @"^[^\s@]+@[^\s@]+\.[A-Za-z]{2,}$",
        ErrorMessage = "Enter a valid email like you@example.com.")]
    [StringLength(256, ErrorMessage = "Email must be 256 characters or fewer.")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Enter a password.")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.RequirementMessage)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.RequirementMessage)]
    public string Password { get; set; } = default!;

    [Required(ErrorMessage = "Choose whether you are a customer or a provider.")]
    [StringLength(50)]
    public string Role { get; set; } = default!;

    [Required(ErrorMessage = "Enter your city.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "City must be between 2 and 80 characters.")]
    public string City { get; set; } = default!;

    [Required(ErrorMessage = PhoneRules.RequirementMessage)]
    [PhoneNumber]
    public string PhoneNumber { get; set; } = default!;

    [Range(0, 80, ErrorMessage = "Years of experience must be between 0 and 80.")]
    public int? YearsOfExperience { get; set; }

    [StringLength(1000, ErrorMessage = "Bio must be 1000 characters or fewer.")]
    public string? Bio { get; set; }
}
