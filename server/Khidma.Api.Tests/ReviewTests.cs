using System.Net;
using System.Net.Http.Json;
using Khidma.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class ReviewTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ReviewTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompletedBooking_CanBeReviewed_AndRatingRecomputes()
    {
        var scenario = await CompletedAsync();
        var response = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/review",
            new { rating = 5, comment = "Excellent work" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.ProviderProfiles.SingleAsync(p => p.UserId == scenario.ProviderUserId);
        Assert.Equal(1, profile.ReviewCount);
        Assert.Equal(5.00m, profile.AverageRating);
    }

    [Fact]
    public async Task IncompleteBooking_CannotBeReviewed()
    {
        var scenario = await CompletedAsync(complete: false);
        var response = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/review",
            new { rating = 4 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task WrongCustomer_CannotReview()
    {
        var scenario = await CompletedAsync();
        var (other, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Hebron");
        var response = await other.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/review",
            new { rating = 3 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecondReview_IsRejected()
    {
        var scenario = await CompletedAsync();
        var first = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/review",
            new { rating = 4 });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/review",
            new { rating = 5 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RatingOutsideRange_IsRejected()
    {
        var scenario = await CompletedAsync();
        var response = await scenario.Customer.PostAsJsonAsync(
            $"/api/bookings/{scenario.BookingId}/review",
            new { rating = 6 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AverageRating_UsesAllReviews()
    {
        var first = await CompletedAsync();
        var firstReview = await first.Customer.PostAsJsonAsync(
            $"/api/bookings/{first.BookingId}/review",
            new { rating = 5 });
        Assert.Equal(HttpStatusCode.OK, firstReview.StatusCode);

        var city = $"R{Guid.NewGuid():N}"[..10];
        var (customerTwo, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.ProviderProfiles.SingleAsync(p => p.UserId == first.ProviderUserId);
            if (!string.Equals(profile.City, city, StringComparison.OrdinalIgnoreCase))
            {
                profile.City = city;
                await db.SaveChangesAsync();
            }
        }

        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var requestId = await TestHarness.CreateRequestAsync(customerTwo, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(first.Provider, requestId);
        var accept = await customerTwo.PostAsync($"/api/offers/{offerId}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var doc = System.Text.Json.JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = doc.RootElement.GetProperty("id").GetInt32();
        await first.Provider.PostAsync($"/api/bookings/{bookingId}/start", null);
        await first.Provider.PostAsync($"/api/bookings/{bookingId}/complete", null);
        var secondReview = await customerTwo.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/review",
            new { rating = 3 });
        Assert.Equal(HttpStatusCode.OK, secondReview.StatusCode);

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await verifyDb.ProviderProfiles.SingleAsync(p => p.UserId == first.ProviderUserId);
        Assert.Equal(2, updated.ReviewCount);
        Assert.Equal(4.00m, updated.AverageRating);
    }

    private async Task<Scenario> CompletedAsync(bool complete = true)
    {
        var city = $"R{Guid.NewGuid():N}"[..10];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);
        var accept = await customer.PostAsync($"/api/offers/{offerId}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var doc = System.Text.Json.JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = doc.RootElement.GetProperty("id").GetInt32();
        if (complete)
        {
            await provider.PostAsync($"/api/bookings/{bookingId}/start", null);
            await provider.PostAsync($"/api/bookings/{bookingId}/complete", null);
        }

        return new Scenario(customer, provider, providerUser.Id, bookingId);
    }

    private sealed record Scenario(
        HttpClient Customer,
        HttpClient Provider,
        string ProviderUserId,
        int BookingId);
}
