using Microsoft.AspNetCore.Identity;

namespace Khidma.Api.Auth;

public static class PasswordRules
{
    public const int MinLength = 8;

    public const string Pattern =
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$";

    public const string RequirementMessage =
        "Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a number, and a special character.";

    public static void Apply(PasswordOptions options)
    {
        options.RequiredLength = MinLength;
        options.RequireDigit = true;
        options.RequireLowercase = true;
        options.RequireUppercase = true;
        options.RequireNonAlphanumeric = true;
        options.RequiredUniqueChars = 1;
    }
}
