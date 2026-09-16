using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class SuspensionTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public SuspensionTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanSuspendApprovedProvider_WithReason()
    {
        var scenario = await ApprovedProviderAsync();
        var response = await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "Professional conduct review" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isSuspended").GetBoolean());
    }

    [Fact]
    public async Task Suspension_RequiresReason()
    {
        var scenario = await ApprovedProviderAsync();
        var response = await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_CannotSuspend()
    {
        var scenario = await ApprovedProviderAsync();
        var response = await scenario.Provider.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "self" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuspendedProvider_IsExcludedFromFeed_AndCannotOffer()
    {
        var city = UniqueCity();
        var scenario = await ApprovedProviderAsync(city);
        var requestId = await TestHarness.CreateRequestAsync(
            scenario.Customer,
            scenario.ServiceId,
            city);
        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "Conduct review" });

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        available.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == requestId);

        var offer = await scenario.Provider.PostAsJsonAsync(
            $"/api/service-requests/{requestId}/offers",
            new
            {
                price = 80,
                message = "Should be blocked",
                estimatedDate = TestHarness.FutureDate()
            });
        Assert.Equal(HttpStatusCode.Forbidden, offer.StatusCode);
    }

    [Fact]
    public async Task Suspension_RejectsPendingOffers_LeavesAcceptedAndBookings()
    {
        var city = UniqueCity();
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (other, otherUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, otherUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);

        var openRequest = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Open job");
        var pendingOffer = await TestHarness.SubmitOfferAsync(provider, openRequest, 70);

        var bookedRequest = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Booked job");
        var acceptedOffer = await TestHarness.SubmitOfferAsync(provider, bookedRequest, 90);
        var accept = await customer.PostAsync($"/api/offers/{acceptedOffer}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var bookingDoc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = bookingDoc.RootElement.GetProperty("id").GetInt32();

        var otherRequest = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Other");
        await TestHarness.SubmitOfferAsync(other, otherRequest, 55);

        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = true, reason = "Conduct review" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = await db.Offers.SingleAsync(o => o.Id == pendingOffer);
        var accepted = await db.Offers.SingleAsync(o => o.Id == acceptedOffer);
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(OfferStatus.Rejected, pending.Status);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
        Assert.Equal(BookingStatus.Scheduled, booking.Status);

        var start = await provider.PostAsync($"/api/bookings/{bookingId}/start", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var complete = await provider.PostAsync($"/api/bookings/{bookingId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
    }

    [Fact]
    public async Task Reactivation_RestoresEligibility_IfStillApproved()
    {
        var city = UniqueCity();
        var scenario = await ApprovedProviderAsync(city);
        var requestId = await TestHarness.CreateRequestAsync(
            scenario.Customer,
            scenario.ServiceId,
            city);
        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "Temporary hold" });
        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = false });

        var available = await scenario.Provider.GetAsync("/api/service-requests/available");
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.Contains(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == requestId);
    }

    [Fact]
    public async Task Reactivation_DoesNotApprovePendingProvider()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = true, reason = "Hold" });
        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = false });
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("isSuspended").GetBoolean());
        Assert.Equal("PendingReview", doc.RootElement.GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task ExistingInProgressBooking_RemainsAfterSuspension()
    {
        var city = UniqueCity();
        var scenario = await ApprovedProviderAsync(city);
        var requestId = await TestHarness.CreateRequestAsync(
            scenario.Customer,
            scenario.ServiceId,
            city);
        var offerId = await TestHarness.SubmitOfferAsync(scenario.Provider, requestId);
        var accept = await scenario.Customer.PostAsync($"/api/offers/{offerId}/accept", null);
        using var bookingDoc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = bookingDoc.RootElement.GetProperty("id").GetInt32();
        await scenario.Provider.PostAsync($"/api/bookings/{bookingId}/start", null);

        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "Hold" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.InProgress, booking.Status);
    }

    private async Task<Scenario> ApprovedProviderAsync(string? city = null)
    {
        city ??= UniqueCity();
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        return new Scenario(admin, customer, provider, profileId, serviceId);
    }

    private static string UniqueCity() => $"S{Guid.NewGuid():N}"[..10];

    private sealed record Scenario(
        HttpClient Admin,
        HttpClient Customer,
        HttpClient Provider,
        int ProfileId,
        int ServiceId);
}
