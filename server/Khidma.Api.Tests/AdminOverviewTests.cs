using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class AdminOverviewEndpointTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AdminOverviewEndpointTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonAdmin_CannotReadOverview()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var response = await customer.GetAsync("/api/admin/stats/overview?range=7d");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("year")]
    [InlineData("1d")]
    public async Task InvalidRange_Returns400(string? range)
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var url = range is null
            ? "/api/admin/stats/overview"
            : $"/api/admin/stats/overview?range={Uri.EscapeDataString(range)}";
        var response = await admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("today")]
    [InlineData("7d")]
    [InlineData("30d")]
    public async Task ValidRange_Returns200(string range)
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var response = await admin.GetAsync($"/api/admin/stats/overview?range={range}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(range, doc.RootElement.GetProperty("range").GetString());
        Assert.Equal(range == "today" ? "hour" : "day", doc.RootElement.GetProperty("bucket").GetString());
    }

    [Fact]
    public async Task CsrfRejection_IsCountedInSecurity24h()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        customer.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var missing = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title = "Missing CSRF",
            description = "This mutation must be rejected without an anti-forgery token.",
            city = "Ramallah",
            preferredDate = TestHarness.FutureDate()
        });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == AuditActions.CsrfRejected));
        }

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(
            doc.RootElement.GetProperty("security24h").GetProperty("csrfRejections").GetInt32() >= 1);
    }
}

public sealed class AdminOverviewConversionTests : IAsyncLifetime
{
    private readonly KhidmaApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task ConversionAndFunnel_UseCreatedInRangeAndReachedBooked()
    {
        var city = $"C{Guid.NewGuid():N}"[..10];
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);

        var openNoOffer = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Open no offer");
        var openWithOffer = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Open with offer");
        var bookedId = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Booked request");
        var completedId = await TestHarness.CreateRequestAsync(
            customer,
            serviceId,
            city,
            "Completed request");

        await TestHarness.SubmitOfferAsync(provider, openWithOffer);
        var bookedOffer = await TestHarness.SubmitOfferAsync(provider, bookedId);
        var completedOffer = await TestHarness.SubmitOfferAsync(provider, completedId);

        (await customer.PostAsync($"/api/offers/{bookedOffer}/accept", null)).EnsureSuccessStatusCode();
        var acceptCompleted = await customer.PostAsync($"/api/offers/{completedOffer}/accept", null);
        acceptCompleted.EnsureSuccessStatusCode();
        using var bookingDoc = JsonDocument.Parse(await acceptCompleted.Content.ReadAsStringAsync());
        var bookingId = bookingDoc.RootElement.GetProperty("id").GetInt32();
        (await provider.PostAsync($"/api/bookings/{bookingId}/start", null)).EnsureSuccessStatusCode();
        (await provider.PostAsync($"/api/bookings/{bookingId}/complete", null)).EnsureSuccessStatusCode();

        Assert.NotEqual(0, openNoOffer);

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var funnel = doc.RootElement.GetProperty("funnel");
        Assert.Equal(4, funnel.GetProperty("requestsCreated").GetInt32());
        Assert.Equal(3, funnel.GetProperty("requestsWithOffer").GetInt32());
        Assert.Equal(2, funnel.GetProperty("booked").GetInt32());
        Assert.Equal(1, funnel.GetProperty("completed").GetInt32());

        var kpis = doc.RootElement.GetProperty("kpis");
        Assert.Equal(0.5m, kpis.GetProperty("conversionRate").GetProperty("value").GetDecimal());
        Assert.Equal(90m, kpis.GetProperty("bookingValue").GetProperty("value").GetDecimal());
        Assert.Equal(1, kpis.GetProperty("bookingsCompleted").GetProperty("value").GetDecimal());
        Assert.Equal(1, kpis.GetProperty("bookingsActive").GetProperty("value").GetDecimal());
    }
}

public sealed class AdminOverviewAttentionTests : IAsyncLifetime
{
    private readonly KhidmaApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task StaleAndOverdueRules_MatchSpec()
    {
        var city = $"T{Guid.NewGuid():N}"[..10];
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (_, pendingUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);

        var freshOpen = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Fresh open");
        var staleOpen = await TestHarness.CreateRequestAsync(customer, serviceId, city, "Stale open");
        var staleWithOffer = await TestHarness.CreateRequestAsync(
            customer,
            serviceId,
            city,
            "Stale with offer");
        var staleWithdrawn = await TestHarness.CreateRequestAsync(
            customer,
            serviceId,
            city,
            "Stale withdrawn");
        var overdueRequest = await TestHarness.CreateRequestAsync(
            customer,
            serviceId,
            city,
            "Overdue booking");

        await TestHarness.SubmitOfferAsync(provider, staleWithOffer);
        var withdrawnOffer = await TestHarness.SubmitOfferAsync(provider, staleWithdrawn);
        (await provider.PostAsync($"/api/offers/{withdrawnOffer}/withdraw", null)).EnsureSuccessStatusCode();

        var overdueOffer = await TestHarness.SubmitOfferAsync(provider, overdueRequest);
        var accept = await customer.PostAsync($"/api/offers/{overdueOffer}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var bookingDoc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = bookingDoc.RootElement.GetProperty("id").GetInt32();

        var staleAt = DateTimeOffset.UtcNow.AddHours(-49);
        await SetRequestCreatedAt(staleOpen, staleAt);
        await SetRequestCreatedAt(staleWithOffer, staleAt);
        await SetRequestCreatedAt(staleWithdrawn, staleAt);
        await SetBookingScheduledDate(bookingId, DateTimeOffset.UtcNow.AddHours(-2));

        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, pendingUser.Id);
        (await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = true, reason = "Hold for review" })).EnsureSuccessStatusCode();

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var attention = doc.RootElement.GetProperty("attention");
        Assert.Equal(1, attention.GetProperty("pendingVerifications").GetInt32());
        Assert.Equal(2, attention.GetProperty("staleOpenRequests").GetInt32());
        Assert.Equal(1, attention.GetProperty("overdueBookings").GetInt32());
        Assert.Equal(1, attention.GetProperty("suspendedProviders").GetInt32());
        Assert.NotEqual(0, freshOpen);
    }

    private async Task SetRequestCreatedAt(int requestId, DateTimeOffset createdAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await db.ServiceRequests.SingleAsync(r => r.Id == requestId);
        request.CreatedAt = createdAt;
        await db.SaveChangesAsync();
    }

    private async Task SetBookingScheduledDate(int bookingId, DateTimeOffset scheduledDate)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        booking.ScheduledDate = scheduledDate;
        await db.SaveChangesAsync();
    }
}

public sealed class AdminOverviewSupplyDemandTests : IAsyncLifetime
{
    private readonly KhidmaApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task SupplyDemand_CountsOnlyEligibleProviders()
    {
        var city = $"S{Guid.NewGuid():N}"[..10];
        var otherCity = $"O{Guid.NewGuid():N}"[..10];
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (eligible, eligibleUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (pending, pendingUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (suspended, suspendedUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (wrongCity, wrongCityUser) = await TestHarness.RegisterAsync(_factory, "Provider", otherCity);
        var (wrongService, wrongServiceUser) = await TestHarness.RegisterAsync(
            _factory,
            "Provider",
            city);

        var plumbingId = await TestHarness.GetServiceIdAsync(_factory);
        var categories = await admin.GetFromJsonAsync<JsonElement>("/api/catalog/categories");
        var categoryId = categories.EnumerateArray().First().GetProperty("id").GetInt32();
        var createElectrical = await admin.PostAsJsonAsync("/api/admin/services", new
        {
            name = $"Electrical {Guid.NewGuid():N}"[..14],
            categoryId
        });
        createElectrical.EnsureSuccessStatusCode();
        using var serviceDoc = JsonDocument.Parse(await createElectrical.Content.ReadAsStringAsync());
        var electricalId = serviceDoc.RootElement.GetProperty("id").GetInt32();

        await TestHarness.ApproveProviderAsync(_factory, eligibleUser.Id, city, plumbingId);
        await TestHarness.ApproveProviderAsync(_factory, suspendedUser.Id, city, plumbingId);
        await TestHarness.ApproveProviderAsync(_factory, wrongCityUser.Id, otherCity, plumbingId);
        await TestHarness.ApproveProviderAsync(_factory, wrongServiceUser.Id, city, electricalId);
        await TestHarness.ApproveProviderAsync(_factory, pendingUser.Id, city, plumbingId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pendingProfile = await db.ProviderProfiles.SingleAsync(p => p.UserId == pendingUser.Id);
            pendingProfile.VerificationStatus = ProviderVerificationStatus.PendingReview;
            var suspendedProfile = await db.ProviderProfiles.SingleAsync(
                p => p.UserId == suspendedUser.Id);
            suspendedProfile.IsSuspended = true;
            await db.SaveChangesAsync();
        }

        await TestHarness.CreateRequestAsync(customer, plumbingId, city, "Need plumber one");
        await TestHarness.CreateRequestAsync(customer, plumbingId, city, "Need plumber two");

        var mixedCity = city.ToUpperInvariant();
        await TestHarness.CreateRequestAsync(customer, plumbingId, mixedCity, "Need plumber mixed");

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = doc.RootElement.GetProperty("supplyDemand").EnumerateArray().ToList();
        var match = rows.Single(row =>
            row.GetProperty("serviceId").GetInt32() == plumbingId &&
            string.Equals(row.GetProperty("city").GetString(), city, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, match.GetProperty("openRequests").GetInt32());
        Assert.Equal(1, match.GetProperty("eligibleProviders").GetInt32());
        Assert.NotNull(eligible);
    }
}

public sealed class AdminOverviewEmptyDatabaseTests : IAsyncLifetime
{
    private readonly KhidmaApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task EmptyMarketplace_ReturnsZeros()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var kpis = root.GetProperty("kpis");
        Assert.Equal(0, kpis.GetProperty("bookingValue").GetProperty("value").GetDecimal());
        Assert.Equal(0, kpis.GetProperty("bookingsActive").GetProperty("value").GetDecimal());
        Assert.Equal(0, kpis.GetProperty("bookingsCompleted").GetProperty("value").GetDecimal());
        Assert.Equal(0, kpis.GetProperty("conversionRate").GetProperty("value").GetDecimal());
        Assert.Equal(0, kpis.GetProperty("avgProviderRating").GetProperty("value").GetDecimal());

        var funnel = root.GetProperty("funnel");
        Assert.Equal(0, funnel.GetProperty("requestsCreated").GetInt32());
        Assert.Equal(0, funnel.GetProperty("requestsWithOffer").GetInt32());
        Assert.Equal(0, funnel.GetProperty("booked").GetInt32());
        Assert.Equal(0, funnel.GetProperty("completed").GetInt32());

        var attention = root.GetProperty("attention");
        Assert.Equal(0, attention.GetProperty("pendingVerifications").GetInt32());
        Assert.Equal(0, attention.GetProperty("staleOpenRequests").GetInt32());
        Assert.Equal(0, attention.GetProperty("overdueBookings").GetInt32());
        Assert.Equal(0, attention.GetProperty("suspendedProviders").GetInt32());

        Assert.Equal(0, root.GetProperty("supplyDemand").GetArrayLength());
        Assert.Equal(0, root.GetProperty("topProviders").GetArrayLength());
        Assert.True(root.GetProperty("bookingsSeries").GetArrayLength() >= 1);
        foreach (var point in root.GetProperty("bookingsSeries").EnumerateArray())
        {
            Assert.Equal(0, point.GetProperty("created").GetInt32());
            Assert.Equal(0, point.GetProperty("completed").GetInt32());
            Assert.Equal(0, point.GetProperty("cancelled").GetInt32());
        }

        var security = root.GetProperty("security24h");
        Assert.Equal(0, security.GetProperty("failedLogins").GetInt32());
        Assert.Equal(0, security.GetProperty("deniedActions").GetInt32());
        Assert.Equal(0, security.GetProperty("csrfRejections").GetInt32());
    }
}
