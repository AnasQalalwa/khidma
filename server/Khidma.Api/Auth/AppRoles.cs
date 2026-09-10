namespace Khidma.Api.Auth;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Provider = "Provider";

    public static readonly string[] All =
    [
        Admin,
        Customer,
        Provider
    ];

    public static bool TryNormalizePublicRole(
        string? role,
        out string normalized)
    {
        if (string.Equals(role, Customer, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Customer;
            return true;
        }

        if (string.Equals(role, Provider, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Provider;
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    public static bool IsAdminRole(string? role) =>
        string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase);
}
