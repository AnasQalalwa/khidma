using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class AccountTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AccountTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithoutPhone_IsRejected()
    {
        var client = TestHarness.CreateClient(_factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "No Phone",
            email = TestHarness.UniqueEmail("nophone"),
            password = "ValidPass1!",
            role = "Customer",
            city = "Ramallah"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CanUpdateProfile_AndChangePassword()
    {
        var (customer, user) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var profile = await customer.GetAsync("/api/account/profile");
        profile.EnsureSuccessStatusCode();
        using var current = JsonDocument.Parse(await profile.Content.ReadAsStringAsync());
        Assert.Equal("Ramallah", current.RootElement.GetProperty("city").GetString());
        Assert.Equal("+970 0591000000", current.RootElement.GetProperty("phoneNumber").GetString());
        Assert.Equal(user.Email, current.RootElement.GetProperty("email").GetString());

        var update = await customer.PutAsJsonAsync("/api/account/profile", new
        {
            fullName = "Updated Customer",
            phoneNumber = "+970 0599111122",
            city = "Nablus"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var updated = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        Assert.Equal("Updated Customer", updated.RootElement.GetProperty("fullName").GetString());
        Assert.Equal("+970 0599111122", updated.RootElement.GetProperty("phoneNumber").GetString());
        Assert.Equal("Nablus", updated.RootElement.GetProperty("city").GetString());

        var wrong = await customer.PostAsJsonAsync("/api/account/password", new
        {
            currentPassword = "WrongPass1!",
            newPassword = "NextPass1!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var changed = await customer.PostAsJsonAsync("/api/account/password", new
        {
            currentPassword = "ValidPass1!",
            newPassword = "NextPass1!"
        });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        await customer.PostAsync("/api/auth/logout", null);
        await AntiforgeryTestHelper.AttachTokenAsync(customer);
        var login = await customer.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = "NextPass1!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Customer_CanSave_ANonPalestinePhone_AndPalestineNumbersKeepTheirShape()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var rejected = await customer.PutAsJsonAsync("/api/account/profile", new
        {
            fullName = "Updated Customer",
            phoneNumber = "+970 599111222",
            city = "Ramallah"
        });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var update = await customer.PutAsJsonAsync("/api/account/profile", new
        {
            fullName = "Updated Customer",
            phoneNumber = "+962 0791234567",
            city = "Ramallah"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var updated = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        Assert.Equal("+962 0791234567", updated.RootElement.GetProperty("phoneNumber").GetString());
    }
}
