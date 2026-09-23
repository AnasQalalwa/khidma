using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class FullDirectBookingWorkflowTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public FullDirectBookingWorkflowTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_ToReview_CompletesForCustomerAndProvider()
    {
        var city = $"W{Guid.NewGuid():N}"[..10];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);

        var service = await customer.GetAsync($"/api/catalog/services/{serviceId}");
        Assert.Equal(HttpStatusCode.OK, service.StatusCode);

        var providers = await customer.GetAsync(
            $"/api/catalog/services/{serviceId}/providers?city={Uri.EscapeDataString(city)}");
        providers.EnsureSuccessStatusCode();
        using var providerDoc = JsonDocument.Parse(await providers.Content.ReadAsStringAsync());
        Assert.Contains(
            providerDoc.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == profileId);

        var bookingId = await TestHarness.CreateBookingAsync(
            customer,
            profileId,
            serviceId,
            "Need the sink fixed tomorrow.");

        var dashboard = await provider.GetFromJsonAsync<JsonElement>("/api/dashboard/provider");
        Assert.True(dashboard.GetProperty("pendingRequestCount").GetInt32() >= 1);
        Assert.Contains(
            dashboard.GetProperty("recentPendingRequests").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == bookingId);

        await TestHarness.AcceptBookingAsync(provider, bookingId, 110);
        (await provider.PostAsync($"/api/bookings/{bookingId}/start", null)).EnsureSuccessStatusCode();
        (await provider.PostAsync($"/api/bookings/{bookingId}/complete", null)).EnsureSuccessStatusCode();

        var review = await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/review", new
        {
            rating = 5,
            comment = "Fixed it the same day."
        });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);

        var detail = await customer.GetFromJsonAsync<JsonElement>($"/api/bookings/{bookingId}");
        Assert.Equal("Completed", detail.GetProperty("status").GetString());
        Assert.Equal(110m, detail.GetProperty("quotedPrice").GetDecimal());
        Assert.Equal(5, detail.GetProperty("review").GetProperty("rating").GetInt32());
        Assert.False(detail.GetProperty("canReview").GetBoolean());
    }
}
