using Khidma.Api.Contracts.Auth;
using Microsoft.AspNetCore.Identity;

namespace Khidma.Api.Auth;

public sealed class RegisterResult
{
    public bool Succeeded { get; private init; }

    public CurrentUserDto? User { get; private init; }

    public int StatusCode { get; private init; }

    public string Title { get; private init; } = "Bad Request";

    public Dictionary<string, string[]> Errors { get; private init; } = [];

    public static RegisterResult Ok(CurrentUserDto user) => new()
    {
        Succeeded = true,
        User = user,
        StatusCode = StatusCodes.Status200OK
    };

    public static RegisterResult FromIdentityErrors(
        IEnumerable<IdentityError> errors)
    {
        var list = errors.ToList();
        var duplicate = list.Any(e =>
            e.Code is "DuplicateEmail" or "DuplicateUserName");

        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var error in list)
        {
            var key = MapKey(error.Code);
            if (!dict.TryGetValue(key, out var existing))
            {
                dict[key] = [error.Description];
            }
            else
            {
                dict[key] = [.. existing, error.Description];
            }
        }

        return new RegisterResult
        {
            Succeeded = false,
            StatusCode = duplicate
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest,
            Title = duplicate
                ? "An account with this email already exists."
                : "Registration failed.",
            Errors = dict
        };
    }

    private static string MapKey(string code)
    {
        if (code.Contains("Email", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            return "email";
        }

        if (code.Contains("Password", StringComparison.OrdinalIgnoreCase))
        {
            return "password";
        }

        return "general";
    }
}
