using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Infrastructure;

namespace Khidma.Api.Tests;

public sealed class VerificationDocumentTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public VerificationDocumentTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Provider_RegistersAsPendingReview()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me/verification");
        Assert.Equal("PendingReview", me.GetProperty("verificationStatus").GetString());
        Assert.False(me.GetProperty("isSuspended").GetBoolean());
        Assert.Equal(0, me.GetProperty("documents").GetArrayLength());
    }

    [Fact]
    public async Task Provider_CanUploadPdfJpegAndPng()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");

        await TestHarness.UploadDocumentAsync(provider, file: TestHarness.PdfContent(), fileName: "a.pdf");
        await TestHarness.UploadDocumentAsync(
            provider,
            "ProfessionalLicense",
            TestHarness.JpegContent(),
            "b.jpg");
        await TestHarness.UploadDocumentAsync(
            provider,
            "TrainingCertificate",
            TestHarness.PngContent(),
            "c.png");

        var me = await provider.GetFromJsonAsync<JsonElement>("/api/providers/me/verification");
        Assert.Equal(3, me.GetProperty("documents").GetArrayLength());
    }

    [Fact]
    public async Task Exe_IsRejected()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var bytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        var response = await PostFileAsync(provider, bytes, "tool.exe", "application/octet-stream");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WrongExtension_IsRejected()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var response = await PostFileAsync(
            provider,
            "%PDF-1.4\n"u8.ToArray(),
            "script.js",
            "application/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FakeMimeSignature_IsRejected()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var response = await PostFileAsync(provider, jpeg, "fake.pdf", "application/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FileOver10Mb_IsRejected()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var bytes = new byte[DocumentFileValidator.MaxFileSizeBytes + 1];
        "%PDF"u8.CopyTo(bytes);
        var response = await PostFileAsync(provider, bytes, "huge.pdf", "application/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnotherProvider_CannotReadOrDeleteDocument()
    {
        var (owner, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var (other, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Nablus");
        var documentId = await TestHarness.UploadDocumentAsync(owner);

        var download = await other.GetAsync(
            $"/api/providers/me/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.Forbidden, download.StatusCode);

        var delete = await other.DeleteAsync(
            $"/api/providers/me/verification-documents/{documentId}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotAccessProviderDocument()
    {
        var (owner, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var documentId = await TestHarness.UploadDocumentAsync(owner);

        var download = await customer.GetAsync(
            $"/api/providers/me/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.Forbidden, download.StatusCode);

        var adminDownload = await customer.GetAsync(
            $"/api/admin/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.Forbidden, adminDownload.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotAccessDocument()
    {
        var (owner, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var documentId = await TestHarness.UploadDocumentAsync(owner);
        var anonymous = TestHarness.CreateClient(_factory);

        var providerDownload = await anonymous.GetAsync(
            $"/api/providers/me/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.Unauthorized, providerDownload.StatusCode);

        var adminDownload = await anonymous.GetAsync(
            $"/api/admin/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.Unauthorized, adminDownload.StatusCode);
    }

    [Fact]
    public async Task OwnerAndAdmin_CanDownload()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (owner, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var documentId = await TestHarness.UploadDocumentAsync(owner);

        var own = await owner.GetAsync(
            $"/api/providers/me/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);

        var adminDownload = await admin.GetAsync(
            $"/api/admin/verification-documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.OK, adminDownload.StatusCode);
    }

    [Fact]
    public async Task Provider_CanDeletePending_ButNotReviewedDocument()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var pendingId = await TestHarness.UploadDocumentAsync(provider);
        var reviewedId = await TestHarness.UploadDocumentAsync(
            provider,
            "PortfolioEvidence",
            TestHarness.JpegContent(),
            "shot.jpg");
        await TestHarness.ApproveDocumentAsync(admin, reviewedId);

        var deletePending = await provider.DeleteAsync(
            $"/api/providers/me/verification-documents/{pendingId}");
        Assert.Equal(HttpStatusCode.OK, deletePending.StatusCode);

        var deleteReviewed = await provider.DeleteAsync(
            $"/api/providers/me/verification-documents/{reviewedId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteReviewed.StatusCode);
    }

    [Fact]
    public async Task RejectedDocument_RequiresNote()
    {
        var (admin, _) = await TestHarness.CreateAdminAsync(_factory);
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var documentId = await TestHarness.UploadDocumentAsync(provider);

        var missing = await admin.PostAsJsonAsync(
            $"/api/admin/verification-documents/{documentId}/review",
            new { status = "Rejected" });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var rejected = await admin.PostAsJsonAsync(
            $"/api/admin/verification-documents/{documentId}/review",
            new { status = "Rejected", note = "Certificate is expired." });
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostFileAsync(
        HttpClient provider,
        byte[] bytes,
        string fileName,
        string contentType)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Other"), "documentType" }
        };
        form.Add(TestHarness.FileContent(bytes, fileName, contentType), "file", fileName);
        return await provider.PostAsync("/api/providers/me/verification-documents", form);
    }
}
