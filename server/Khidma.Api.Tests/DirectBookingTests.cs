using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class DirectBookingTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public DirectBookingTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Customer_CanBookApprovedProvider_AndBothSeePhones()
    {
        var scenario = await ReadyAsync();
        var response = await scenario.Customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = scenario.ProfileId,
            serviceId = scenario.ServiceId,
            requestedDate = TestHarness.FutureDay(),
            notes = "Leaky kitchen tap"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Pending", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("+970 0591000000", doc.RootElement.GetProperty("customerPhone").GetString());
        Assert.Equal("+970 0591000000", doc.RootElement.GetProperty("providerPhone").GetString());
        Assert.True(doc.RootElement.GetProperty("canCancel").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("canAccept").GetBoolean());

        var bookingId = doc.RootElement.GetProperty("id").GetInt32();
        var providerView = await scenario.Provider.GetAsync($"/api/bookings/{bookingId}");
        providerView.EnsureSuccessStatusCode();
        using var providerDoc = JsonDocument.Parse(await providerView.Content.ReadAsStringAsync());
        Assert.Equal("+970 0591000000", providerDoc.RootElement.GetProperty("customerPhone").GetString());
        Assert.True(providerDoc.RootElement.GetProperty("canAccept").GetBoolean());
        Assert.True(providerDoc.RootElement.GetProperty("canDecline").GetBoolean());
        Assert.False(providerDoc.RootElement.GetProperty("canCancel").GetBoolean());
    }

    [Fact]
    public async Task PastDate_IsRejected()
    {
        var scenario = await ReadyAsync();
        var response = await scenario.Customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = scenario.ProfileId,
            serviceId = scenario.ServiceId,
            requestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnapprovedProvider_CannotBeBooked()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (_, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        var response = await customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = profileId,
            serviceId,
            requestedDate = TestHarness.FutureDay()
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DuplicatePendingBooking_IsRejected()
    {
        var scenario = await ReadyAsync();
        await TestHarness.CreateBookingAsync(scenario.Customer, scenario.ProfileId, scenario.ServiceId);
        var response = await scenario.Customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = scenario.ProfileId,
            serviceId = scenario.ServiceId,
            requestedDate = TestHarness.FutureDay()
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_QuotesPrice_AndSchedules()
    {
        var scenario = await ReadyAsync();
        var bookingId = await TestHarness.CreateBookingAsync(
            scenario.Customer,
            scenario.ProfileId,
            scenario.ServiceId);
        var response = await scenario.Provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/accept",
            new
            {
                price = 150,
                message = "I can come in the morning.",
                scheduledStart = TestHarness.SlotStart(bookingId),
                durationHours = 2
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Scheduled", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(150m, doc.RootElement.GetProperty("quotedPrice").GetDecimal());
        Assert.Equal("I can come in the morning.", doc.RootElement.GetProperty("providerMessage").GetString());
        Assert.True(doc.RootElement.GetProperty("canStart").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("canCancel").GetBoolean());
    }

    [Fact]
    public async Task Decline_RequiresReason_AndEndsTheRequest()
    {
        var scenario = await ReadyAsync();
        var bookingId = await TestHarness.CreateBookingAsync(
            scenario.Customer,
            scenario.ProfileId,
            scenario.ServiceId);
        var missing = await scenario.Provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/decline",
            new { reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var response = await scenario.Provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/decline",
            new { reason = "Not available that day" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Declined", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Not available that day", doc.RootElement.GetProperty("declineReason").GetString());
    }

    [Fact]
    public async Task Customer_CanCancelPending_ProviderCannot()
    {
        var scenario = await ReadyAsync();
        var bookingId = await TestHarness.CreateBookingAsync(
            scenario.Customer,
            scenario.ProfileId,
            scenario.ServiceId);

        var providerCancel = await scenario.Provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/cancel",
            new { reason = "Cannot make it" });
        Assert.Equal(HttpStatusCode.Conflict, providerCancel.StatusCode);

        var customerCancel = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/cancel",
            new { reason = "Found someone else" });
        Assert.Equal(HttpStatusCode.OK, customerCancel.StatusCode);
    }

    [Fact]
    public async Task Stranger_CannotSeePhones()
    {
        var scenario = await ReadyAsync();
        var bookingId = await TestHarness.CreateBookingAsync(
            scenario.Customer,
            scenario.ProfileId,
            scenario.ServiceId);
        var (other, _) = await TestHarness.RegisterAsync(_factory, "Customer", UniqueCity());
        var response = await other.GetAsync($"/api/bookings/{bookingId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Scenario> ReadyAsync()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        return new Scenario(customer, provider, profileId, serviceId);
    }

    private static string UniqueCity() => $"D{Guid.NewGuid():N}"[..10];

    private sealed record Scenario(
        HttpClient Customer,
        HttpClient Provider,
        int ProfileId,
        int ServiceId);
}
