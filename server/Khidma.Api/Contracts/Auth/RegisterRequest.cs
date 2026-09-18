using System.ComponentModel.DataAnnotations;

namespace Khidma.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = default!;

    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = default!;

    [Range(0, 80)]
    public int? YearsOfExperience { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }
}
