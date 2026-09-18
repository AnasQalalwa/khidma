using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class ServiceRequestTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ServiceRequestTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Customer_CanCreateValidRequest()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);

        var response = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title = "Kitchen leak",
            description = "There is water under the sink.",
            city = "Ramallah",
            preferredDate = TestHarness.FutureDate(),
            budgetMin = 40,
            budgetMax = 90
        });

        var createBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, createBody);
        using var doc = JsonDocument.Parse(createBody);
        Assert.Equal("Open", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Kitchen leak", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PreferredDateInPast_IsRejected()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);

        var response = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title = "Old job",
            description = "This date is in the past.",
            city = "Ramallah",
            preferredDate = DateTimeOffset.UtcNow.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBudget_IsRejected()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);

        var response = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title = "Budget mismatch",
            description = "Max is lower than min.",
            city = "Ramallah",
            preferredDate = TestHarness.FutureDate(),
            budgetMin = 100,
            budgetMax = 20
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonOwner_CannotEdit()
    {
        var (owner, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var (other, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var requestId = await TestHarness.CreateRequestAsync(owner, serviceId, "Ramallah");

        var response = await other.PutAsJsonAsync($"/api/service-requests/{requestId}", new
        {
            title = "Hijacked",
            description = "Should not work.",
            preferredDate = TestHarness.FutureDate()
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenRequestWithoutActiveOffers_CanEdit()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, "Ramallah");

        var response = await customer.PutAsJsonAsync($"/api/service-requests/{requestId}", new
        {
            title = "Updated title",
            description = "Updated description for the leak.",
            preferredDate = TestHarness.FutureDate().AddDays(1),
            budgetMin = 60,
            budgetMax = 140
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Updated title", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task RequestWithActiveOffer_CannotEdit()
    {
        var city = $"City{Guid.NewGuid():N}"[..12];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        await TestHarness.SubmitOfferAsync(provider, requestId);

        var response = await customer.PutAsJsonAsync($"/api/service-requests/{requestId}", new
        {
            title = "Should fail",
            description = "Active offer blocks edits.",
            preferredDate = TestHarness.FutureDate()
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CanCancelOpenRequest_AndPendingOffersAreRejected()
    {
        var city = $"City{Guid.NewGuid():N}"[..12];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);

        var response = await customer.PostAsync($"/api/service-requests/{requestId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await db.ServiceRequests.SingleAsync(r => r.Id == requestId);
        var offer = await db.Offers.SingleAsync(o => o.Id == offerId);
        Assert.Equal(ServiceRequestStatus.Cancelled, request.Status);
        Assert.Equal(OfferStatus.Rejected, offer.Status);
    }
}
