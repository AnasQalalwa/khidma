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
    public async Task SuspendedProvider_IsHiddenFromCatalog_AndCannotBeBooked()
    {
        var city = UniqueCity();
        var scenario = await ApprovedProviderAsync(city);
        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "Conduct review" });

        var list = await scenario.Customer.GetAsync(
            $"/api/catalog/services/{scenario.ServiceId}/providers?city={Uri.EscapeDataString(city)}");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            doc.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.ProfileId);

        var book = await scenario.Customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = scenario.ProfileId,
            serviceId = scenario.ServiceId,
            requestedDate = TestHarness.FutureDay()
        });
        Assert.Equal(HttpStatusCode.Conflict, book.StatusCode);
    }

    [Fact]
    public async Task Suspension_DeclinesPendingBookings_LeavesScheduled()
    {
        var city = UniqueCity();
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (_, otherUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, otherUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        var otherProfileId = await TestHarness.GetProviderProfileIdAsync(_factory, otherUser.Id);

        var scheduledId = await TestHarness.CreateBookingAsync(customer, profileId, serviceId);
        await TestHarness.AcceptBookingAsync(provider, scheduledId);
        var pendingId = await TestHarness.CreateBookingAsync(customer, profileId, serviceId);
        var otherPendingId = await TestHarness.CreateBookingAsync(customer, otherProfileId, serviceId);

        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = true, reason = "Conduct review" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = await db.Bookings.SingleAsync(b => b.Id == pendingId);
        var scheduled = await db.Bookings.SingleAsync(b => b.Id == scheduledId);
        var otherPending = await db.Bookings.SingleAsync(b => b.Id == otherPendingId);
        Assert.Equal(BookingStatus.Declined, pending.Status);
        Assert.Equal(BookingStatus.Scheduled, scheduled.Status);
        Assert.Equal(BookingStatus.Pending, otherPending.Status);

        var start = await provider.PostAsync($"/api/bookings/{scheduledId}/start", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var complete = await provider.PostAsync($"/api/bookings/{scheduledId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
    }

    [Fact]
    public async Task Reactivation_RestoresEligibility_IfStillApproved()
    {
        var city = UniqueCity();
        var scenario = await ApprovedProviderAsync(city);
        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = true, reason = "Temporary hold" });
        await scenario.Admin.PostAsJsonAsync(
            $"/api/admin/providers/{scenario.ProfileId}/suspension",
            new { suspended = false });

        var available = await scenario.Customer.GetAsync(
            $"/api/catalog/services/{scenario.ServiceId}/providers?city={Uri.EscapeDataString(city)}");
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.Contains(
            doc.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == scenario.ProfileId);
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
        var bookingId = await TestHarness.CreateBookingAsync(
            scenario.Customer,
            scenario.ProfileId,
            scenario.ServiceId);
        await TestHarness.AcceptBookingAsync(scenario.Provider, bookingId);
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
