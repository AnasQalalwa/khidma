using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class AuditLogTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AuditLogTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthEvents_AreRecorded_WithoutSecrets()
    {
        var client = TestHarness.CreateClient(_factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var email = TestHarness.UniqueEmail("audit");
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Audit User",
            email,
            password = "ValidPass1!",
            role = "Customer",
            city = "Ramallah"
        });
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        await client.PostAsync("/api/auth/logout", null);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPass1!"
        });
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "ValidPass1!"
        });

        var actions = await ActionsAsync();
        Assert.Contains(AuditActions.RegisterSucceeded, actions);
        Assert.Contains(AuditActions.Logout, actions);
        Assert.Contains(AuditActions.LoginFailed, actions);
        Assert.Contains(AuditActions.LoginSucceeded, actions);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var json = string.Join(" ", await db.AuditLogs.Select(a => a.DetailsJson ?? "").ToListAsync());
        Assert.DoesNotContain("ValidPass1!", json, StringComparison.Ordinal);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("XSRF", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".AspNetCore", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarketplaceAndAdminEvents_AreRecorded()
    {
        var city = $"A{Guid.NewGuid():N}"[..10];
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        var documentId = await TestHarness.UploadDocumentAsync(provider);
        await TestHarness.ApproveDocumentAsync(admin, documentId);
        await TestHarness.ApproveProviderViaApiAsync(admin, profileId);
        await provider.PutAsJsonAsync("/api/providers/me/services", new { serviceIds = new[] { serviceId } });
        await provider.PutAsJsonAsync("/api/providers/me", new
        {
            city,
            yearsOfExperience = 4,
            bio = "Updated"
        });

        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerId = await TestHarness.SubmitOfferAsync(provider, requestId);
        var accept = await customer.PostAsync($"/api/offers/{offerId}/accept", null);
        using var bookingDoc = JsonDocument.Parse(await accept.Content.ReadAsStringAsync());
        var bookingId = bookingDoc.RootElement.GetProperty("id").GetInt32();
        await provider.PostAsync($"/api/bookings/{bookingId}/start", null);
        await provider.PostAsync($"/api/bookings/{bookingId}/complete", null);
        await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/review", new
        {
            rating = 5,
            comment = "Excellent"
        });

        var cancelCity = $"B{Guid.NewGuid():N}"[..10];
        var cancelRequest = await TestHarness.CreateRequestAsync(
            customer,
            serviceId,
            city,
            "Will cancel");
        await customer.PostAsync($"/api/service-requests/{cancelRequest}/cancel", null);

        var createCategory = await admin.PostAsJsonAsync("/api/admin/categories", new
        {
            name = $"Audit {Guid.NewGuid():N}"[..12]
        });
        createCategory.EnsureSuccessStatusCode();

        var actions = await ActionsAsync();
        Assert.Contains(AuditActions.DocumentUploaded, actions);
        Assert.Contains(AuditActions.AdminDocumentReviewed, actions);
        Assert.Contains(AuditActions.ProviderApproved, actions);
        Assert.Contains(AuditActions.ProviderProfileUpdated, actions);
        Assert.Contains(AuditActions.ProviderServicesUpdated, actions);
        Assert.Contains(AuditActions.RequestCreated, actions);
        Assert.Contains(AuditActions.RequestCancelled, actions);
        Assert.Contains(AuditActions.OfferSubmitted, actions);
        Assert.Contains(AuditActions.OfferAccepted, actions);
        Assert.Contains(AuditActions.BookingCreated, actions);
        Assert.Contains(AuditActions.BookingStarted, actions);
        Assert.Contains(AuditActions.BookingCompleted, actions);
        Assert.Contains(AuditActions.ReviewCreated, actions);
        Assert.Contains(AuditActions.CategoryCreated, actions);
    }

    [Fact]
    public async Task AuditEndpoints_EnforceAuthorization_AndHaveNoDelete()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var anonymous = TestHarness.CreateClient(_factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/admin/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync("/api/admin/audit-logs")).StatusCode);

        var ok = await admin.GetAsync("/api/admin/audit-logs");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var first = JsonDocument.Parse(await ok.Content.ReadAsStringAsync())
            .RootElement.GetProperty("items")[0].GetProperty("id").GetInt64();
        var detail = await admin.GetAsync($"/api/admin/audit-logs/{first}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        var delete = await admin.DeleteAsync($"/api/admin/audit-logs/{first}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task SuspensionAndRejection_RecordAudit()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Rejected", reason = "Need license" });
        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = true, reason = "Hold" });
        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/suspension",
            new { suspended = false });

        var actions = await ActionsAsync();
        Assert.Contains(AuditActions.ProviderRejected, actions);
        Assert.Contains(AuditActions.ProviderSuspended, actions);
        Assert.Contains(AuditActions.ProviderReactivated, actions);
    }

    private async Task<HashSet<string>> ActionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.AuditLogs.Select(a => a.Action).ToListAsync()).ToHashSet();
    }
}
