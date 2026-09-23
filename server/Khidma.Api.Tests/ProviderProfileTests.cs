using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class ProviderProfileTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ProviderProfileTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Provider_CanUpdateProfile_ButNotApprovalOrRating()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var response = await provider.PutAsJsonAsync("/api/providers/me", new
        {
            city = "Jenin",
            yearsOfExperience = 8,
            bio = "Updated bio",
            phoneNumber = "+970 0592000000"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Jenin", doc.RootElement.GetProperty("city").GetString());
        Assert.Equal(8, doc.RootElement.GetProperty("yearsOfExperience").GetInt32());
        Assert.Equal("PendingReview", doc.RootElement.GetProperty("verificationStatus").GetString());
        Assert.Equal(0, doc.RootElement.GetProperty("reviewCount").GetInt32());
    }

    [Fact]
    public async Task Provider_CanSaveMapCoordinates()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var response = await provider.PutAsJsonAsync("/api/providers/me", new
        {
            city = "Ramallah",
            latitude = 31.9038m,
            longitude = 35.2034m,
            yearsOfExperience = 4,
            bio = "Pinned on the map",
            phoneNumber = "+970 0592000000"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Ramallah", doc.RootElement.GetProperty("city").GetString());
        Assert.Equal(31.9038m, doc.RootElement.GetProperty("latitude").GetDecimal());
        Assert.Equal(35.2034m, doc.RootElement.GetProperty("longitude").GetDecimal());
    }

    [Fact]
    public async Task Provider_CanReplaceServices()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var response = await provider.PutAsJsonAsync("/api/providers/me/services", new
        {
            serviceIds = new[] { serviceId }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("services").GetArrayLength());
    }

    [Fact]
    public async Task PublicProfile_DoesNotExposeEmail()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me");
        var id = me.GetProperty("id").GetInt32();
        var email = me.GetProperty("email").GetString();

        var anonymous = TestHarness.CreateClient(_factory);
        var publicProfile = await anonymous.GetAsync($"/api/providers/{id}");
        Assert.Equal(HttpStatusCode.OK, publicProfile.StatusCode);
        var body = await publicProfile.Content.ReadAsStringAsync();
        Assert.DoesNotContain(email!, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", body, StringComparison.OrdinalIgnoreCase);
    }
}
