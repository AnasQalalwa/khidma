using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class AdminTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AdminTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanReadStats()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var response = await admin.GetAsync("/api/admin/stats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("customers").GetInt32() >= 0);
        Assert.True(doc.RootElement.GetProperty("providers").GetInt32() >= 0);
        Assert.True(doc.RootElement.GetProperty("categories").GetInt32() >= 1);
        Assert.True(doc.RootElement.GetProperty("services").GetInt32() >= 1);
    }

    [Fact]
    public async Task NonAdmin_CannotAccessStats()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var response = await customer.GetAsync("/api/admin/stats");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanApproveProvider()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me");
        var profileId = me.GetProperty("id").GetInt32();

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/approval",
            new { isApproved = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isApproved").GetBoolean());
    }

    [Fact]
    public async Task Admin_CannotDeleteCategoryWithServices()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var categories = await admin.GetFromJsonAsync<JsonElement>("/api/catalog/categories");
        var id = categories.EnumerateArray().First().GetProperty("id").GetInt32();

        var response = await admin.DeleteAsync($"/api/admin/categories/{id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateAndDeleteUnusedCategory()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var create = await admin.PostAsJsonAsync("/api/admin/categories", new
        {
            name = $"Temp {Guid.NewGuid():N}"[..12]
        });
        create.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = doc.RootElement.GetProperty("id").GetInt32();

        var delete = await admin.DeleteAsync($"/api/admin/categories/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }
}
