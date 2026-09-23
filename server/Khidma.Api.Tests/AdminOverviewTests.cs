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
        var missing = await customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = 1,
            serviceId,
            requestedDate = TestHarness.FutureDay(),
            notes = "This mutation must be rejected without an anti-forgery token."
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
        var providers = new List<(HttpClient Client, int ProfileId)>();
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        for (var i = 0; i < 4; i++)
        {
            var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
            await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
            var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
            providers.Add((provider, profileId));
        }

        var pendingId = await TestHarness.CreateBookingAsync(customer, providers[0].ProfileId, serviceId);
        var declinedId = await TestHarness.CreateBookingAsync(customer, providers[1].ProfileId, serviceId);
        (await providers[1].Client.PostAsJsonAsync(
            $"/api/bookings/{declinedId}/decline",
            new { reason = "Fully booked" })).EnsureSuccessStatusCode();
        var scheduledId = await TestHarness.CreateBookingAsync(customer, providers[2].ProfileId, serviceId);
        await TestHarness.AcceptBookingAsync(providers[2].Client, scheduledId);
        var completedId = await TestHarness.CreateBookingAsync(customer, providers[3].ProfileId, serviceId);
        await TestHarness.AcceptBookingAsync(providers[3].Client, completedId);
        (await providers[3].Client.PostAsync($"/api/bookings/{completedId}/start", null)).EnsureSuccessStatusCode();
        (await providers[3].Client.PostAsync($"/api/bookings/{completedId}/complete", null)).EnsureSuccessStatusCode();

        Assert.NotEqual(0, pendingId);

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var funnel = doc.RootElement.GetProperty("funnel");
        Assert.Equal(4, funnel.GetProperty("requested").GetInt32());
        Assert.Equal(2, funnel.GetProperty("accepted").GetInt32());
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
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var providers = new List<(HttpClient Client, string UserId, int ProfileId)>();
        for (var i = 0; i < 4; i++)
        {
            var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
            await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
            var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
            providers.Add((provider, providerUser.Id, profileId));
        }

        var (_, pendingUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var freshPending = await TestHarness.CreateBookingAsync(customer, providers[0].ProfileId, serviceId);
        var stalePending = await TestHarness.CreateBookingAsync(customer, providers[1].ProfileId, serviceId);
        var secondStale = await TestHarness.CreateBookingAsync(customer, providers[2].ProfileId, serviceId);
        var overdueId = await TestHarness.CreateBookingAsync(customer, providers[3].ProfileId, serviceId);
        await TestHarness.AcceptBookingAsync(providers[3].Client, overdueId);

        var staleAt = DateTimeOffset.UtcNow.AddHours(-49);
        await SetBookingCreatedAt(stalePending, staleAt);
        await SetBookingCreatedAt(secondStale, staleAt);
        await SetBookingScheduledStart(overdueId, DateTimeOffset.UtcNow.AddHours(-2));

        var pendingProfileId = await TestHarness.GetProviderProfileIdAsync(_factory, pendingUser.Id);
        (await admin.PostAsJsonAsync(
            $"/api/admin/providers/{pendingProfileId}/suspension",
            new { suspended = true, reason = "Hold for review" })).EnsureSuccessStatusCode();

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var attention = doc.RootElement.GetProperty("attention");
        Assert.Equal(1, attention.GetProperty("pendingVerifications").GetInt32());
        Assert.Equal(2, attention.GetProperty("stalePendingBookings").GetInt32());
        Assert.Equal(1, attention.GetProperty("overdueBookings").GetInt32());
        Assert.Equal(1, attention.GetProperty("suspendedProviders").GetInt32());
        Assert.NotEqual(0, freshPending);
    }

    private async Task SetBookingCreatedAt(int bookingId, DateTimeOffset createdAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        booking.CreatedAt = createdAt;
        await db.SaveChangesAsync();
    }

    private async Task SetBookingScheduledStart(int bookingId, DateTimeOffset scheduledStart)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        booking.ScheduledStart = scheduledStart;
        booking.DurationHours = 2;
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
        var (customer, customerUser) = await TestHarness.RegisterAsync(_factory, "Customer", city);
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
            description = "Safe electrical checks and repairs.",
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

        var bookable = new[] { eligibleUser, suspendedUser, wrongCityUser };
        foreach (var user in bookable)
        {
            var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, user.Id);
            await TestHarness.CreateBookingAsync(customer, profileId, plumbingId);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pendingProfile = await db.ProviderProfiles.SingleAsync(p => p.UserId == pendingUser.Id);
            pendingProfile.VerificationStatus = ProviderVerificationStatus.PendingReview;
            var suspendedProfile = await db.ProviderProfiles.SingleAsync(
                p => p.UserId == suspendedUser.Id);
            suspendedProfile.IsSuspended = true;
            var mixed = await db.Bookings
                .Where(b => b.CustomerId == customerUser.Id)
                .OrderByDescending(b => b.Id)
                .FirstAsync();
            mixed.City = city.ToUpperInvariant();
            await db.SaveChangesAsync();
        }

        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = doc.RootElement.GetProperty("supplyDemand").EnumerateArray().ToList();
        var match = rows.Single(row =>
            row.GetProperty("serviceId").GetInt32() == plumbingId &&
            string.Equals(row.GetProperty("city").GetString(), city, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, match.GetProperty("pendingBookings").GetInt32());
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
        Assert.Equal(0, funnel.GetProperty("requested").GetInt32());
        Assert.Equal(0, funnel.GetProperty("accepted").GetInt32());
        Assert.Equal(0, funnel.GetProperty("completed").GetInt32());

        var attention = root.GetProperty("attention");
        Assert.Equal(0, attention.GetProperty("pendingVerifications").GetInt32());
        Assert.Equal(0, attention.GetProperty("stalePendingBookings").GetInt32());
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

public sealed class AdminOverviewTopProvidersTests : IAsyncLifetime
{
    private readonly KhidmaApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task RejectedProvider_WithFiveStarRating_IsNotListed()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        await SeedNamedProviderAsync(
            "Rejected Five Star",
            ProviderVerificationStatus.Rejected,
            suspended: false,
            rating: 5.00m,
            reviewCount: 4);

        Assert.DoesNotContain("Rejected Five Star", await TopProviderNamesAsync(admin));
    }

    [Fact]
    public async Task SuspendedProvider_IsNotListed()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        await SeedNamedProviderAsync(
            "Suspended Star",
            ProviderVerificationStatus.Approved,
            suspended: true,
            rating: 4.90m,
            reviewCount: 6);

        Assert.DoesNotContain("Suspended Star", await TopProviderNamesAsync(admin));
    }

    [Fact]
    public async Task PendingProvider_IsNotListed()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        await SeedNamedProviderAsync(
            "Pending Star",
            ProviderVerificationStatus.PendingReview,
            suspended: false,
            rating: 4.80m,
            reviewCount: 3);

        Assert.DoesNotContain("Pending Star", await TopProviderNamesAsync(admin));
    }

    [Fact]
    public async Task ApprovedProvider_WithZeroReviewsAndZeroCompletedBookings_IsNotListed()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        await SeedNamedProviderAsync(
            "Idle Approved",
            ProviderVerificationStatus.Approved,
            suspended: false,
            rating: 0m,
            reviewCount: 0);

        Assert.DoesNotContain("Idle Approved", await TopProviderNamesAsync(admin));
    }

    [Fact]
    public async Task TopProviders_OrderByRatingThenReviewCount()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        await SeedNamedProviderAsync(
            "Lower Rated Many Reviews",
            ProviderVerificationStatus.Approved,
            suspended: false,
            rating: 4.50m,
            reviewCount: 20);
        await SeedNamedProviderAsync(
            "Top Rated Few Reviews",
            ProviderVerificationStatus.Approved,
            suspended: false,
            rating: 5.00m,
            reviewCount: 2);
        await SeedNamedProviderAsync(
            "Top Rated Many Reviews",
            ProviderVerificationStatus.Approved,
            suspended: false,
            rating: 5.00m,
            reviewCount: 8);

        var names = await TopProviderNamesAsync(admin);
        Assert.Equal(
            new[] { "Top Rated Many Reviews", "Top Rated Few Reviews", "Lower Rated Many Reviews" },
            names);
    }

    private async Task SeedNamedProviderAsync(
        string fullName,
        ProviderVerificationStatus status,
        bool suspended,
        decimal rating,
        int reviewCount)
    {
        var city = $"P{Guid.NewGuid():N}"[..10];
        var (_, user) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.ProviderProfiles
            .Include(p => p.User)
            .SingleAsync(p => p.UserId == user.Id);
        profile.User.FullName = fullName;
        profile.VerificationStatus = status;
        profile.IsSuspended = suspended;
        profile.AverageRating = rating;
        profile.ReviewCount = reviewCount;
        await db.SaveChangesAsync();
    }

    private static async Task<List<string>> TopProviderNamesAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/admin/stats/overview?range=7d");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("topProviders")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString() ?? string.Empty)
            .ToList();
    }
}
