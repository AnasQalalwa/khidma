using Khidma.Api.Auth;

namespace Khidma.Api.Tests;

public sealed class AppRolesTests
{
    [Theory]
    [InlineData("Customer", "Customer")]
    [InlineData("customer", "Customer")]
    [InlineData("PROVIDER", "Provider")]
    public void TryNormalizePublicRole_AcceptsCustomerAndProvider(
        string input,
        string expected)
    {
        var ok = AppRoles.TryNormalizePublicRole(input, out var normalized);
        Assert.True(ok);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("")]
    [InlineData(null)]
    public void TryNormalizePublicRole_RejectsAdminAndUnknown(string? input)
    {
        var ok = AppRoles.TryNormalizePublicRole(input, out var normalized);
        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void IsAdminRole_DetectsAdminRegardlessOfCase()
    {
        Assert.True(AppRoles.IsAdminRole("Admin"));
        Assert.True(AppRoles.IsAdminRole("admin"));
        Assert.False(AppRoles.IsAdminRole("Customer"));
    }
}
