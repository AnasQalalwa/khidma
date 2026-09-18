using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class FullMarketplaceWorkflowTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public FullMarketplaceWorkflowTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullSuccessfulMarketplaceWorkflow_UpdatesRatingsAndAudit()
    {
        var city = $"W{Guid.NewGuid():N}"[..10];
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);

        var documentId = await TestHarness.UploadDocumentAsync(provider);
        await TestHarness.ApproveDocumentAsync(admin, documentId);
        await TestHarness.ApproveProviderViaApiAsync(admin, profileId);
        await provider.PutAsJsonAsync("/api/providers/me", new
        {
            city,
            yearsOfExperience = 5,
            bio = "Licensed plumber"
        });
        await provider.PutAsJsonAsync("/api/providers/me/services", new { serviceIds = new[] { serviceId } });

        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var available = await provider.GetAsync("/api/service-requests/available");
        available.EnsureSuccessStatusCode();
        using (var availableDoc = JsonDocument.Parse(await available.Content.ReadAsStringAsync()))
        {
            Assert.Contains(
                availableDoc.RootElement.GetProperty("items").EnumerateArray(),
                item => item.GetProperty("id").GetInt32() == requestId);
        }

        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId, 95);
        var offers = await customer.GetAsync($"/api/service-requests/{requestId}/offers");
        offers.EnsureSuccessStatusCode();
        using (var offersDoc = JsonDocument.Parse(await offers.Content.ReadAsStringAsync()))
        {
            Assert.Contains(
                offersDoc.RootElement.EnumerateArray(),
                item => item.GetProperty("id").GetInt32() == offerId);
        }

        var accept = await customer.PostAsync($"/api/offers/{offerId}/accept", null);
        accept.EnsureSuccessStatusCode();
        using var bookingDoc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = bookingDoc.RootElement.GetProperty("id").GetInt32();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.Bookings.CountAsync(b => b.ServiceRequestId == requestId));
        }

        (await provider.PostAsync($"/api/bookings/{bookingId}/start", null)).EnsureSuccessStatusCode();
        (await provider.PostAsync($"/api/bookings/{bookingId}/complete", null)).EnsureSuccessStatusCode();
        var review = await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/review", new
        {
            rating = 5,
            comment = "Outstanding work"
        });
        review.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.ProviderProfiles.SingleAsync(p => p.Id == profileId);
            Assert.Equal(1, profile.ReviewCount);
            Assert.Equal(5.00m, profile.AverageRating);

            var actions = await db.AuditLogs.Select(a => a.Action).ToListAsync();
            Assert.Contains(AuditActions.DocumentUploaded, actions);
            Assert.Contains(AuditActions.ProviderApproved, actions);
            Assert.Contains(AuditActions.RequestCreated, actions);
            Assert.Contains(AuditActions.OfferSubmitted, actions);
            Assert.Contains(AuditActions.OfferAccepted, actions);
            Assert.Contains(AuditActions.BookingStarted, actions);
            Assert.Contains(AuditActions.BookingCompleted, actions);
            Assert.Contains(AuditActions.ReviewCreated, actions);
        }
    }
}

public sealed class CsrfMutationTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public CsrfMutationTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StateChangingRequest_WithoutXsrf_IsRejected_WithTokenSucceeds()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);

        customer.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var missing = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title = "No CSRF token",
            description = "This mutation must be rejected without an anti-forgery token.",
            city = "Ramallah",
            preferredDate = TestHarness.FutureDate()
        });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        await AntiforgeryTestHelper.AttachTokenAsync(customer);
        var created = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title = "With CSRF token",
            description = "This mutation should succeed with a valid anti-forgery token.",
            city = "Ramallah",
            preferredDate = TestHarness.FutureDate(),
            budgetMin = 40,
            budgetMax = 90
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
    }
}
