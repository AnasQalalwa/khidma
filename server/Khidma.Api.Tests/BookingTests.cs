using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class BookingTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public BookingTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Scheduled_ToInProgress_IsLegal()
    {
        var scenario = await BookedAsync();
        var response = await scenario.Provider.PostAsync(
            $"/api/bookings/{scenario.BookingId}/start",
            null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InProgress", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task InProgress_ToCompleted_CompletesRequest()
    {
        var scenario = await BookedAsync();
        await scenario.Provider.PostAsync($"/api/bookings/{scenario.BookingId}/start", null);
        var response = await scenario.Provider.PostAsync(
            $"/api/bookings/{scenario.BookingId}/complete",
            null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == scenario.BookingId);
        var request = await db.ServiceRequests.SingleAsync(r => r.Id == scenario.RequestId);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(ServiceRequestStatus.Completed, request.Status);
    }

    [Fact]
    public async Task Scheduled_ToCancelled_CancelsRequest()
    {
        var scenario = await BookedAsync();
        var response = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/cancel",
            new { reason = "Plans changed" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == scenario.BookingId);
        var request = await db.ServiceRequests.SingleAsync(r => r.Id == scenario.RequestId);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(ServiceRequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public async Task Scheduled_ToCompleted_IsIllegal()
    {
        var scenario = await BookedAsync();
        var response = await scenario.Provider.PostAsync(
            $"/api/bookings/{scenario.BookingId}/complete",
            null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Completed_ToInProgress_IsIllegal()
    {
        var scenario = await BookedAsync();
        await scenario.Provider.PostAsync($"/api/bookings/{scenario.BookingId}/start", null);
        await scenario.Provider.PostAsync($"/api/bookings/{scenario.BookingId}/complete", null);
        var response = await scenario.Provider.PostAsync(
            $"/api/bookings/{scenario.BookingId}/start",
            null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task InProgress_ToCancelled_IsIllegal()
    {
        var scenario = await BookedAsync();
        await scenario.Provider.PostAsync($"/api/bookings/{scenario.BookingId}/start", null);
        var response = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/cancel",
            new { reason = "Too late" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Completed_ToCancelled_IsIllegal()
    {
        var scenario = await BookedAsync();
        await scenario.Provider.PostAsync($"/api/bookings/{scenario.BookingId}/start", null);
        await scenario.Provider.PostAsync($"/api/bookings/{scenario.BookingId}/complete", null);
        var response = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/cancel",
            new { reason = "After complete" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnrelatedCustomer_CannotAccessBooking()
    {
        var scenario = await BookedAsync();
        var (other, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Nablus");
        var response = await other.GetAsync($"/api/bookings/{scenario.BookingId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotStartBooking()
    {
        var scenario = await BookedAsync();
        var response = await scenario.Customer.PostAsync(
            $"/api/bookings/{scenario.BookingId}/start",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Scenario> BookedAsync()
    {
        var city = $"B{Guid.NewGuid():N}"[..10];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);
        var accept = await customer.PostAsync($"/api/offers/{offerId}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        return new Scenario(
            customer,
            provider,
            requestId,
            doc.RootElement.GetProperty("id").GetInt32());
    }

    private sealed record Scenario(
        HttpClient Customer,
        HttpClient Provider,
        int RequestId,
        int BookingId);
}
