using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class ProviderVerificationDecisionTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ProviderVerificationDecisionTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProviderWithZeroDocuments_CannotBeApproved()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = (await provider.GetFromJsonAsync<JsonElement>("/api/providers/me"))
            .GetProperty("id")
            .GetInt32();

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Approved" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProviderWithOnlyPendingDocuments_CannotBeApproved()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = (await provider.GetFromJsonAsync<JsonElement>("/api/providers/me"))
            .GetProperty("id")
            .GetInt32();
        await TestHarness.UploadDocumentAsync(provider);

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Approved" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProviderWithOnlyRejectedDocuments_CannotBeApproved()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = (await provider.GetFromJsonAsync<JsonElement>("/api/providers/me"))
            .GetProperty("id")
            .GetInt32();
        var documentId = await TestHarness.UploadDocumentAsync(provider);
        await admin.PostAsJsonAsync(
            $"/api/admin/verification-documents/{documentId}/review",
            new { status = "Rejected", note = "Unreadable scan." });

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Approved" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProviderWithApprovedDocument_CanBeApproved_AndBecomesEligible()
    {
        var city = UniqueCity();
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var profileId = (await provider.GetFromJsonAsync<JsonElement>("/api/providers/me"))
            .GetProperty("id")
            .GetInt32();
        await provider.PutAsJsonAsync("/api/providers/me/services", new { serviceIds = new[] { serviceId } });
        var documentId = await TestHarness.UploadDocumentAsync(provider);
        await TestHarness.ApproveDocumentAsync(admin, documentId);

        var approved = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Approved" });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var available = await provider.GetAsync("/api/service-requests/available");
        available.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
        Assert.Contains(
            doc.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == requestId);
    }

    [Fact]
    public async Task Rejection_RequiresReason()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = (await provider.GetFromJsonAsync<JsonElement>("/api/providers/me"))
            .GetProperty("id")
            .GetInt32();

        var missing = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Rejected" });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task RejectedProvider_NewUpload_ReturnsToPendingReview()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Hebron");
        var profileId = (await provider.GetFromJsonAsync<JsonElement>("/api/providers/me"))
            .GetProperty("id")
            .GetInt32();
        await admin.PostAsJsonAsync(
            $"/api/admin/providers/{profileId}/verification",
            new { status = "Rejected", reason = "Need a current license." });

        await TestHarness.UploadDocumentAsync(provider);
        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me/verification");
        Assert.Equal("PendingReview", me.GetProperty("verificationStatus").GetString());
        Assert.True(me.GetProperty("verificationRejectionReason").ValueKind is JsonValueKind.Null
            or JsonValueKind.Undefined);
    }

    private static string UniqueCity() => $"V{Guid.NewGuid():N}"[..10];
}
