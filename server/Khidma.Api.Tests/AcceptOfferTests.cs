using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class AcceptOfferTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AcceptOfferTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AcceptOffer_CreatesSingleBooking_AndRejectsSiblings()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (providerOne, userOne) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (providerTwo, userTwo) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, userOne.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, userTwo.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerOne = await TestHarness.SubmitOfferAsync(providerOne, requestId, 90);
        var offerTwo = await TestHarness.SubmitOfferAsync(providerTwo, requestId, 80);

        var accept = await customer.PostAsync($"/api/offers/{offerOne}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        using var doc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        Assert.Equal("Scheduled", doc.RootElement.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookings = await db.Bookings.Where(b => b.ServiceRequestId == requestId).ToListAsync();
        Assert.Single(bookings);
        Assert.Equal(offerOne, bookings[0].OfferId);

        var accepted = await db.Offers.SingleAsync(o => o.Id == offerOne);
        var sibling = await db.Offers.SingleAsync(o => o.Id == offerTwo);
        var request = await db.ServiceRequests.SingleAsync(r => r.Id == requestId);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
        Assert.Equal(OfferStatus.Rejected, sibling.Status);
        Assert.Equal(ServiceRequestStatus.Booked, request.Status);
    }

    [Fact]
    public async Task SecondAcceptance_IsRejected()
    {
        var scenario = await AcceptedScenarioAsync();
        var second = await scenario.Customer.PostAsync(
            $"/api/offers/{scenario.OfferId}/accept",
            null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task NonOwner_CannotAccept()
    {
        var city = UniqueCity();
        var (owner, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (other, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(owner, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);

        var response = await other.PostAsync($"/api/offers/{offerId}/accept", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WrongState_Returns409()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);
        await provider.PostAsync($"/api/offers/{offerId}/withdraw", null);

        var response = await customer.PostAsync($"/api/offers/{offerId}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<Accepted> AcceptedScenarioAsync()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);
        var accept = await customer.PostAsync($"/api/offers/{offerId}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        return new Accepted(
            customer,
            provider,
            offerId,
            doc.RootElement.GetProperty("id").GetInt32());
    }

    private static string UniqueCity() => $"A{Guid.NewGuid():N}"[..10];

    private sealed record Accepted(
        HttpClient Customer,
        HttpClient Provider,
        int OfferId,
        int BookingId);
}
