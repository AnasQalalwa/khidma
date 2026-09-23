using System.ComponentModel.DataAnnotations;
using Khidma.Api.Auth;

namespace Khidma.Api.Contracts.Account;

public sealed class AccountProfileDto
{
    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string PhoneNumber { get; init; }

    public required string Role { get; init; }

    public string? City { get; init; }
}

public sealed class UpdateAccountProfileRequest
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = default!;

    [Required(ErrorMessage = PhoneRules.RequirementMessage)]
    [PhoneNumber]
    public string PhoneNumber { get; set; } = default!;

    [StringLength(80, MinimumLength = 2, ErrorMessage = "City must be between 2 and 80 characters.")]
    public string? City { get; set; }
}

public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Enter your current password.")]
    public string CurrentPassword { get; set; } = default!;

    [Required(ErrorMessage = "Enter a new password.")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.RequirementMessage)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.RequirementMessage)]
    public string NewPassword { get; set; } = default!;
}
