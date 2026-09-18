using System.Net;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class EligibilityTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public EligibilityTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApprovedMatchingProvider_SeesRequest()
    {
        var city = UniqueCity();
        var scenario = await CreateOpenRequestAsync(city);
        await TestHarness.ApproveProviderAsync(
            _factory,
            scenario.ProviderUserId,
            city,
            scenario.ServiceId);

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        Assert.Equal(HttpStatusCode.OK, available.StatusCode);
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.Contains(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.RequestId);
    }

    [Fact]
    public async Task WrongService_DoesNotSeeRequest()
    {
        var city = UniqueCity();
        var scenario = await CreateOpenRequestAsync(city);
        await TestHarness.ApproveProviderAsync(
            _factory,
            scenario.ProviderUserId,
            city);

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.RequestId);
    }

    [Fact]
    public async Task WrongCity_DoesNotSeeRequest()
    {
        var scenario = await CreateOpenRequestAsync(UniqueCity());
        await TestHarness.ApproveProviderAsync(
            _factory,
            scenario.ProviderUserId,
            "OtherCity",
            scenario.ServiceId);

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.RequestId);
    }

    [Fact]
    public async Task UnapprovedProvider_DoesNotSeeRequest()
    {
        var city = UniqueCity();
        var scenario = await CreateOpenRequestAsync(city);
        await TestHarness.ApproveProviderAsync(
            _factory,
            scenario.ProviderUserId,
            city,
            scenario.ServiceId);
        await TestHarness.SetApprovedAsync(_factory, scenario.ProviderUserId, false);

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.RequestId);
    }

    [Fact]
    public async Task ClosedRequest_DoesNotAppear()
    {
        var city = UniqueCity();
        var scenario = await CreateOpenRequestAsync(city);
        await TestHarness.ApproveProviderAsync(
            _factory,
            scenario.ProviderUserId,
            city,
            scenario.ServiceId);
        var cancel = await scenario.Customer.PostAsync(
            $"/api/service-requests/{scenario.RequestId}/cancel",
            null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.RequestId);
    }

    [Fact]
    public async Task IneligibleProvider_DetailIsHidden()
    {
        var scenario = await CreateOpenRequestAsync(UniqueCity());
        var detail = await scenario.Provider.GetAsync(
            $"/api/service-requests/{scenario.RequestId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task Provider_CannotSeeCustomerContactBeforeAcceptance()
    {
        var city = UniqueCity();
        var scenario = await CreateOpenRequestAsync(city);
        await TestHarness.ApproveProviderAsync(
            _factory,
            scenario.ProviderUserId,
            city,
            scenario.ServiceId);

        var detail = await scenario.Provider.GetAsync(
            $"/api/service-requests/{scenario.RequestId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await detail.Content.ReadAsStringAsync();
        Assert.DoesNotContain(scenario.CustomerEmail, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customerEmail", body, StringComparison.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.TryGetProperty("customerEmail", out _));
    }

    private async Task<Scenario> CreateOpenRequestAsync(string city)
    {
        var (customer, customerUser) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);

        return new Scenario(
            customer,
            customerUser.Email,
            provider,
            providerUser.Id,
            serviceId,
            requestId);
    }

    private static string UniqueCity() => $"C{Guid.NewGuid():N}"[..10];

    private sealed record Scenario(
        HttpClient Customer,
        string CustomerEmail,
        HttpClient Provider,
        string ProviderUserId,
        int ServiceId,
        int RequestId);
}
