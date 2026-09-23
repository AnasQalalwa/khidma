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
        var documentId = await TestHarness.UploadDocumentAsync(provider);
        await TestHarness.ApproveDocumentAsync(admin, documentId);

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Approved" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Approved", doc.RootElement.GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task Admin_CannotDeleteCategoryWithServices()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var categories = await admin.GetFromJsonAsync<JsonElement>("/api/catalog/categories");
        var id = categories.EnumerateArray().First().GetProperty("id").GetInt32();

        var response = await admin.DeleteAsync($"/api/admin/categories/{id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot be deleted because it still has", body);
    }

    [Fact]
    public async Task Admin_DeleteBlockReason_NamesProvidersOrBookings()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var assign = await provider.PutAsJsonAsync("/api/providers/me/services", new
        {
            serviceIds = new[] { serviceId }
        });
        assign.EnsureSuccessStatusCode();

        var usage = await admin.GetFromJsonAsync<JsonElement>("/api/admin/catalog/usage");
        var blocked = usage.EnumerateArray()
            .First(item => item.GetProperty("serviceId").GetInt32() == serviceId);
        var reason = blocked.GetProperty("deleteBlockReason").GetString();
        Assert.Contains("1 provider offers it", reason);

        var response = await admin.DeleteAsync($"/api/admin/services/{serviceId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(reason!, body);
    }

    [Fact]
    public async Task Admin_CanCreateAndDeleteUnusedCategory()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var create = await admin.PostAsJsonAsync("/api/admin/categories", new
        {
            name = $"Temp {Guid.NewGuid():N}"[..12],
            description = "Seasonal outdoor help."
        });
        create.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = doc.RootElement.GetProperty("id").GetInt32();

        var delete = await admin.DeleteAsync($"/api/admin/categories/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Admin_CanSaveServiceCopyAndImage()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var categories = await admin.GetFromJsonAsync<JsonElement>("/api/catalog/categories");
        var categoryId = categories.EnumerateArray().First().GetProperty("id").GetInt32();
        var create = await admin.PostAsJsonAsync("/api/admin/services", new
        {
            name = $"Garden {Guid.NewGuid():N}"[..12],
            description = "Lawn care and seasonal planting.",
            categoryId
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("Lawn care and seasonal planting.", created.RootElement.GetProperty("description").GetString());
        Assert.False(created.RootElement.GetProperty("hasImage").GetBoolean());

        using var form = new MultipartFormDataContent();
        form.Add(TestHarness.PngContent("garden.png"), "file", "garden.png");
        var upload = await admin.PostAsync($"/api/admin/services/{id}/image", form);
        upload.EnsureSuccessStatusCode();

        var image = await _factory.CreateClient().GetAsync($"/api/catalog/services/{id}/image");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        var bytes = await image.Content.ReadAsByteArrayAsync();
        Assert.Equal(0x89, bytes[0]);
    }
}
