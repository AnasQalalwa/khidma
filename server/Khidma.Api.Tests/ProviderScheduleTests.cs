using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class ProviderScheduleTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ProviderScheduleTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Provider_CanReplaceWorkingHours_AndCustomerCannot()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);

        var saved = await provider.PutAsJsonAsync("/api/providers/me/working-hours", new
        {
            hours = new[]
            {
                new { dayOfWeek = 0, hour = 9 },
                new { dayOfWeek = 0, hour = 10 },
                new { dayOfWeek = 0, hour = 9 }
            }
        });
        saved.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("hours").GetArrayLength());

        var read = await provider.GetAsync("/api/providers/me/working-hours");
        read.EnsureSuccessStatusCode();

        var denied = await customer.PutAsJsonAsync("/api/providers/me/working-hours", new
        {
            hours = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task ClosedDay_IsRejected_AndAvailabilityHidesBusySlots()
    {
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (otherCustomer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);

        var openDay = DateOnly.Parse(TestHarness.FutureDay());
        var closedDay = openDay.AddDays(1);
        (await provider.PutAsJsonAsync("/api/providers/me/working-hours", new
        {
            hours = new[] { new { dayOfWeek = (int)openDay.DayOfWeek, hour = 9 } }
        })).EnsureSuccessStatusCode();

        var closed = await customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = profileId,
            serviceId,
            requestedDate = closedDay.ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.BadRequest, closed.StatusCode);

        var firstId = await TestHarness.CreateBookingAsync(customer, profileId, serviceId, "First visit");
        var secondId = await TestHarness.CreateBookingAsync(otherCustomer, profileId, serviceId, "Second visit");
        var slot = TestHarness.SlotStart(firstId);
        (await provider.PostAsJsonAsync($"/api/bookings/{firstId}/accept", new
        {
            price = 40,
            scheduledStart = slot,
            durationHours = 3
        })).EnsureSuccessStatusCode();

        var overlap = await provider.PostAsJsonAsync($"/api/bookings/{secondId}/accept", new
        {
            price = 40,
            scheduledStart = slot.AddHours(1),
            durationHours = 2
        });
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);

        var moved = slot.AddDays(30);
        var rescheduled = await provider.PostAsJsonAsync($"/api/bookings/{firstId}/schedule", new
        {
            scheduledStart = moved,
            durationHours = 6,
            note = "The job needs a full day next month."
        });
        rescheduled.EnsureSuccessStatusCode();
        using var detail = JsonDocument.Parse(await rescheduled.Content.ReadAsStringAsync());
        Assert.Equal(6, detail.RootElement.GetProperty("durationHours").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(detail.RootElement.GetProperty("rescheduleNote").GetString()));

        var anon = TestHarness.CreateClient(_factory);
        var from = DateOnly.FromDateTime(DateTime.UtcNow);
        var availability = await anon.GetAsync(
            $"/api/providers/{profileId}/availability?from={from:yyyy-MM-dd}&to={from.AddDays(60):yyyy-MM-dd}");
        availability.EnsureSuccessStatusCode();
        using var avail = JsonDocument.Parse(await availability.Content.ReadAsStringAsync());
        Assert.Equal(1, avail.RootElement.GetProperty("workingHours").GetArrayLength());
        Assert.Contains(
            avail.RootElement.GetProperty("busy").EnumerateArray(),
            item => item.GetProperty("start").GetDateTimeOffset() == moved);

        var tooWide = await anon.GetAsync(
            $"/api/providers/{profileId}/availability?from={from:yyyy-MM-dd}&to={from.AddDays(90):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.BadRequest, tooWide.StatusCode);
    }

    [Fact]
    public async Task UnavailableProvider_AvailabilityIsHidden()
    {
        var city = UniqueCity();
        var (_, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        var from = DateOnly.FromDateTime(DateTime.UtcNow);
        var anon = TestHarness.CreateClient(_factory);
        var response = await anon.GetAsync(
            $"/api/providers/{profileId}/availability?from={from:yyyy-MM-dd}&to={from.AddDays(7):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string UniqueCity() => $"H{Guid.NewGuid():N}"[..10];
}
