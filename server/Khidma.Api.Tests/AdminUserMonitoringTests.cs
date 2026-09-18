using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class AdminUserMonitoringTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AdminUserMonitoringTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanListUsers_WithRoleSearchAndPaging()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, customerUser) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");

        var all = await admin.GetAsync("/api/admin/users?page=1&pageSize=2");
        all.EnsureSuccessStatusCode();
        using var allDoc = JsonDocument.Parse(await all.Content.ReadAsStringAsync());
        Assert.Equal(2, allDoc.RootElement.GetProperty("pageSize").GetInt32());
        Assert.True(allDoc.RootElement.GetProperty("totalCount").GetInt32() >= 3);

        var customers = await admin.GetAsync("/api/admin/users?role=Customer");
        customers.EnsureSuccessStatusCode();
        using var customerDoc = JsonDocument.Parse(await customers.Content.ReadAsStringAsync());
        Assert.All(
            customerDoc.RootElement.GetProperty("items").EnumerateArray(),
            item => Assert.Equal("Customer", item.GetProperty("role").GetString()));

        var search = await admin.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(customerUser.Email)}");
        search.EnsureSuccessStatusCode();
        using var searchDoc = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
        Assert.Contains(
            searchDoc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("email").GetString() == customerUser.Email);

        var forbidden = await customer.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task LastLoginAt_UpdatesOnSuccessfulLogin_Only()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var client = TestHarness.CreateClient(_factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var email = TestHarness.UniqueEmail("login-monitor");
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login Monitor",
            email,
            password = "ValidPass1!",
            role = "Customer",
            city = "Ramallah"
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == email);
            Assert.Null(user.LastLoginAt);
        }

        await AntiforgeryTestHelper.AttachTokenAsync(client);
        await client.PostAsync("/api/auth/logout", null);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPass1!"
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == email);
            Assert.Null(user.LastLoginAt);
        }

        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "ValidPass1!"
        });
        login.EnsureSuccessStatusCode();

        var listed = await admin.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        listed.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        var item = doc.RootElement.GetProperty("items").EnumerateArray().Single();
        Assert.False(item.GetProperty("lastLoginAt").ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
    }
}
