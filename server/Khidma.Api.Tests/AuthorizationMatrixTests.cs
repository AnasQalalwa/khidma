using System.Net;
using System.Net.Http.Json;

namespace Khidma.Api.Tests;

public sealed class AuthorizationMatrixTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AuthorizationMatrixTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_PostServiceRequest_Returns401()
    {
        var client = TestHarness.CreateClient(_factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var response = await client.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId = 1,
            title = "Anon",
            description = "Should require auth.",
            city = "Ramallah",
            preferredDate = TestHarness.FutureDate()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Provider_AccessingCustomerEndpoint_Returns403()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var response = await provider.GetAsync("/api/service-requests/mine");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Customer_AccessingProviderEndpoint_Returns403()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var response = await customer.GetAsync("/api/service-requests/available");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProviderB_CannotWithdrawProviderAOffer()
    {
        var city = $"X{Guid.NewGuid():N}"[..10];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (providerA, userA) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (providerB, userB) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, userA.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, userB.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(providerA, requestId);

        var response = await providerB.PostAsync($"/api/offers/{offerId}/withdraw", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed class PaginationTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public PaginationTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListEndpoints_HonorPageSizeCapAndPaging()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        for (var i = 0; i < 3; i++)
        {
            await TestHarness.CreateRequestAsync(
                customer,
                serviceId,
                "Ramallah",
                $"Paged request {i}");
        }

        var page = await customer.GetAsync("/api/service-requests/mine?page=1&pageSize=2");
        page.EnsureSuccessStatusCode();
        using var doc = System.Text.Json.JsonDocument.Parse(await page.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("items").GetArrayLength());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 3);

        var capped = await customer.GetAsync("/api/service-requests/mine?page=1&pageSize=99");
        capped.EnsureSuccessStatusCode();
        using var cappedDoc = System.Text.Json.JsonDocument.Parse(await capped.Content.ReadAsStringAsync());
        Assert.Equal(50, cappedDoc.RootElement.GetProperty("pageSize").GetInt32());
    }
}
