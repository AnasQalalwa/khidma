using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class OfferTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public OfferTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EligibleProvider_CanOffer()
    {
        var scenario = await EligibleScenarioAsync();
        var response = await scenario.Provider.PostAsJsonAsync(
            $"/api/service-requests/{scenario.RequestId}/offers",
            new
            {
                price = 75,
                message = "I can do this tomorrow.",
                estimatedDate = TestHarness.FutureDate()
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnapprovedProvider_CannotOffer()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        await TestHarness.SetApprovedAsync(_factory, providerUser.Id, false);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);

        var response = await provider.PostAsJsonAsync(
            $"/api/service-requests/{requestId}/offers",
            new
            {
                price = 75,
                message = "Should be hidden.",
                estimatedDate = TestHarness.FutureDate()
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WrongService_CannotOffer()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);

        var response = await provider.PostAsJsonAsync(
            $"/api/service-requests/{requestId}/offers",
            new
            {
                price = 75,
                message = "Wrong trade.",
                estimatedDate = TestHarness.FutureDate()
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateActiveOffer_IsRejected()
    {
        var scenario = await EligibleScenarioAsync();
        await TestHarness.SubmitOfferAsync(scenario.Provider, scenario.RequestId);

        var second = await scenario.Provider.PostAsJsonAsync(
            $"/api/service-requests/{scenario.RequestId}/offers",
            new
            {
                price = 80,
                message = "Second offer",
                estimatedDate = TestHarness.FutureDate()
            });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task OfferOnNonOpenRequest_IsRejected()
    {
        var scenario = await EligibleScenarioAsync();
        var cancel = await scenario.Customer.PostAsync(
            $"/api/service-requests/{scenario.RequestId}/cancel",
            null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var response = await scenario.Provider.PostAsJsonAsync(
            $"/api/service-requests/{scenario.RequestId}/offers",
            new
            {
                price = 75,
                message = "Too late",
                estimatedDate = TestHarness.FutureDate()
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Owner_CanWithdrawPendingOffer()
    {
        var scenario = await EligibleScenarioAsync();
        var offerId = await TestHarness.SubmitOfferAsync(scenario.Provider, scenario.RequestId);

        var response = await scenario.Provider.PostAsync($"/api/offers/{offerId}/withdraw", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Withdrawn", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AnotherProvider_CannotWithdrawOffer()
    {
        var scenario = await EligibleScenarioAsync();
        var offerId = await TestHarness.SubmitOfferAsync(scenario.Provider, scenario.RequestId);
        var (other, otherUser) = await TestHarness.RegisterAsync(_factory, "Provider", scenario.City);
        await TestHarness.ApproveProviderAsync(_factory, otherUser.Id, scenario.City, scenario.ServiceId);

        var response = await other.PostAsync($"/api/offers/{offerId}/withdraw", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonPendingOffer_CannotWithdraw()
    {
        var scenario = await EligibleScenarioAsync();
        var offerId = await TestHarness.SubmitOfferAsync(scenario.Provider, scenario.RequestId);
        await scenario.Provider.PostAsync($"/api/offers/{offerId}/withdraw", null);

        var second = await scenario.Provider.PostAsync($"/api/offers/{offerId}/withdraw", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private async Task<Scenario> EligibleScenarioAsync()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        return new Scenario(customer, provider, city, serviceId, requestId);
    }

    private static string UniqueCity() => $"O{Guid.NewGuid():N}"[..10];

    private sealed record Scenario(
        HttpClient Customer,
        HttpClient Provider,
        string City,
        int ServiceId,
        int RequestId);
}
