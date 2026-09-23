using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Khidma.Api.Tests;

public sealed class ProviderProfileChangeTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ProviderProfileChangeTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApprovedProvider_CannotChangeCityWithoutReview()
    {
        var (provider, user) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        await TestHarness.ApproveProviderAsync(_factory, user.Id, "Nablus");

        var response = await provider.PutAsJsonAsync("/api/providers/me", new
        {
            city = "Jenin",
            yearsOfExperience = 8,
            bio = "Trying to move cities",
            phoneNumber = "+970 0592000000"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ApprovedProvider_CanUpdateBioAndYears()
    {
        var (provider, user) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        await TestHarness.ApproveProviderAsync(_factory, user.Id, "Nablus");

        var response = await provider.PutAsJsonAsync("/api/providers/me", new
        {
            city = "Nablus",
            yearsOfExperience = 12,
            bio = "Updated after approval",
            phoneNumber = "+970 0592000000"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Nablus", doc.RootElement.GetProperty("city").GetString());
        Assert.Equal(12, doc.RootElement.GetProperty("yearsOfExperience").GetInt32());
        Assert.Equal("Updated after approval", doc.RootElement.GetProperty("bio").GetString());
        Assert.False(doc.RootElement.GetProperty("canEditLocation").GetBoolean());
    }

    [Fact]
    public async Task ApprovedProvider_CannotAddServiceWithoutProof()
    {
        var (provider, user) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var plumbing = await TestHarness.GetServiceIdAsync(_factory);
        var electrical = await TestHarness.GetServiceIdAsync(_factory, "Electrical");
        await TestHarness.ApproveProviderAsync(_factory, user.Id, "Nablus", plumbing);

        var response = await provider.PutAsJsonAsync("/api/providers/me/services", new
        {
            serviceIds = new[] { plumbing, electrical }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ApprovedProvider_CanRemoveAService()
    {
        var (provider, user) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var plumbing = await TestHarness.GetServiceIdAsync(_factory);
        var electrical = await TestHarness.GetServiceIdAsync(_factory, "Electrical");
        await TestHarness.ApproveProviderAsync(_factory, user.Id, "Nablus", plumbing, electrical);

        var response = await provider.PutAsJsonAsync("/api/providers/me/services", new
        {
            serviceIds = new[] { plumbing }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("services").GetArrayLength());
        Assert.Equal(plumbing, doc.RootElement.GetProperty("services")[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LocationChange_AppliesOnlyAfterAdminApproval()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, user) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        await TestHarness.ApproveProviderAsync(_factory, user.Id, "Nablus");

        var request = await provider.PostAsJsonAsync("/api/providers/me/location-changes", new
        {
            city = "Ramallah",
            latitude = 31.9038m,
            longitude = 35.2034m
        });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        using var pending = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
        Assert.Equal("Nablus", pending.RootElement.GetProperty("city").GetString());
        var changeId = pending.RootElement.GetProperty("pendingChanges")[0].GetProperty("id").GetInt32();

        var duplicate = await provider.PostAsJsonAsync("/api/providers/me/location-changes", new
        {
            city = "Jenin",
            latitude = 32.46m,
            longitude = 35.3m
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var review = await admin.PostAsJsonAsync($"/api/admin/profile-changes/{changeId}/review", new
        {
            status = "Approved"
        });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);

        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me");
        Assert.Equal("Ramallah", me.GetProperty("city").GetString());
        Assert.Equal(0, me.GetProperty("pendingChanges").GetArrayLength());
    }

    [Fact]
    public async Task AddingAService_RequiresProof_AndGoesLiveWhenDocumentIsApproved()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, user) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var plumbing = await TestHarness.GetServiceIdAsync(_factory);
        var electrical = await TestHarness.GetServiceIdAsync(_factory, "Electrical");
        await TestHarness.ApproveProviderAsync(_factory, user.Id, "Ramallah", plumbing);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(electrical.ToString()), "serviceId" },
            { new StringContent("ProfessionalCertificate"), "documentType" }
        };
        form.Add(TestHarness.PdfContent(), "file", "electrical.pdf");
        var request = await provider.PostAsync("/api/providers/me/service-changes", form);
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        using var pending = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
        Assert.Equal(1, pending.RootElement.GetProperty("services").GetArrayLength());
        Assert.Equal("AddService", pending.RootElement.GetProperty("pendingChanges")[0].GetProperty("type").GetString());

        var verification = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me/verification");
        var documentId = verification.GetProperty("documents")[0].GetProperty("id").GetInt32();
        Assert.Equal(electrical, verification.GetProperty("documents")[0].GetProperty("serviceId").GetInt32());

        await TestHarness.ApproveDocumentAsync(admin, documentId);

        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me");
        Assert.Equal(2, me.GetProperty("services").GetArrayLength());
        Assert.Equal(0, me.GetProperty("pendingChanges").GetArrayLength());
    }

    [Fact]
    public async Task Provider_CanUploadAndServeProfilePhoto()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        using var form = new MultipartFormDataContent();
        form.Add(TestHarness.JpegContent(), "file", "me.jpg");

        var upload = await provider.PostAsync("/api/providers/me/photo", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        using var body = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("hasPhoto").GetBoolean());
        var id = body.RootElement.GetProperty("id").GetInt32();

        var anonymous = TestHarness.CreateClient(_factory);
        var photo = await anonymous.GetAsync($"/api/providers/{id}/photo");
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Equal("image/jpeg", photo.Content.Headers.ContentType?.MediaType);

        var publicProfile = await anonymous.GetFromJsonAsync<JsonElement>($"/api/providers/{id}");
        Assert.True(publicProfile.GetProperty("hasPhoto").GetBoolean());
        Assert.False(publicProfile.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task UnapprovedProvider_CannotSubmitChangeRequests()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var electrical = await TestHarness.GetServiceIdAsync(_factory, "Electrical");

        var location = await provider.PostAsJsonAsync("/api/providers/me/location-changes", new
        {
            city = "Ramallah",
            latitude = 31.9m,
            longitude = 35.2m
        });
        Assert.Equal(HttpStatusCode.Conflict, location.StatusCode);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(electrical.ToString()), "serviceId" },
            { new StringContent("ProfessionalCertificate"), "documentType" }
        };
        form.Add(TestHarness.PdfContent(), "file", "electrical.pdf");
        var service = await provider.PostAsync("/api/providers/me/service-changes", form);
        Assert.Equal(HttpStatusCode.Conflict, service.StatusCode);
    }
}
