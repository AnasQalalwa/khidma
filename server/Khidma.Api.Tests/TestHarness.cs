using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Auth;
using Khidma.Api.Contracts.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

internal static class TestHarness
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    public static string UniqueEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@khidma.test";

    public static DateTimeOffset FutureDate() => DateTimeOffset.UtcNow.AddDays(7);

    public static string FutureDay() => FutureDate().UtcDateTime.ToString("yyyy-MM-dd");

    public static DateTimeOffset SlotStart(int bookingId) =>
        DateTimeOffset.UtcNow.AddDays(20 + (bookingId % 200)).AddHours(9);

    public static async Task<(HttpClient Client, CurrentUserDto User)> RegisterAsync(
        WebApplicationFactory<Program> factory,
        string role,
        string city)
    {
        var client = CreateClient(factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var email = UniqueEmail(role.ToLowerInvariant());
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"Test {role}",
            email,
            password = "ValidPass1!",
            phoneNumber = "+970 0591000000",
            role,
            city,
            yearsOfExperience = 3,
            bio = "Reliable tester"
        });

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>(Json);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        return (client, user!);
    }

    public static async Task<(HttpClient Client, CurrentUserDto User)> CreateAdminAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = UniqueEmail("admin");
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Test Admin",
                CreatedAt = DateTimeOffset.UtcNow
            };

            var created = await userManager.CreateAsync(user, "ValidPass1!");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, AppRoles.Admin);
        }

        var client = CreateClient(factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "ValidPass1!"
        });
        login.EnsureSuccessStatusCode();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/auth/me", Json);
        return (client, me!);
    }

    public static async Task<int> GetServiceIdAsync(
        WebApplicationFactory<Program> factory,
        string name = "Plumbing")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Services
            .Where(s => s.Name == name)
            .Select(s => s.Id)
            .SingleAsync();
    }

    public static async Task ApproveProviderAsync(
        WebApplicationFactory<Program> factory,
        string userId,
        string? city = null,
        params int[] serviceIds)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.ProviderProfiles
            .Include(p => p.ProviderServices)
            .SingleAsync(p => p.UserId == userId);

        profile.VerificationStatus = ProviderVerificationStatus.Approved;
        profile.IsSuspended = false;
        profile.SuspensionReason = null;
        profile.SuspendedAt = null;
        profile.SuspendedByUserId = null;
        if (city is not null)
        {
            profile.City = city;
        }

        if (serviceIds.Length > 0)
        {
            db.ProviderServices.RemoveRange(profile.ProviderServices);
            foreach (var serviceId in serviceIds)
            {
                db.ProviderServices.Add(new ProviderService
                {
                    ProviderProfileId = profile.Id,
                    ServiceId = serviceId
                });
            }
        }

        await EnsureOpenHoursAsync(db, profile.Id);
        await db.SaveChangesAsync();
    }

    public static async Task EnsureOpenHoursAsync(AppDbContext db, int providerProfileId)
    {
        if (await db.ProviderWorkingHours.AnyAsync(h => h.ProviderProfileId == providerProfileId))
        {
            return;
        }

        for (var day = 0; day <= 6; day++)
        {
            for (var hour = 8; hour <= 18; hour++)
            {
                db.ProviderWorkingHours.Add(new ProviderWorkingHour
                {
                    ProviderProfileId = providerProfileId,
                    DayOfWeek = day,
                    Hour = hour
                });
            }
        }
    }

    public static async Task SetApprovedAsync(
        WebApplicationFactory<Program> factory,
        string userId,
        bool isApproved)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.ProviderProfiles.SingleAsync(p => p.UserId == userId);
        profile.VerificationStatus = isApproved
            ? ProviderVerificationStatus.Approved
            : ProviderVerificationStatus.PendingReview;
        await db.SaveChangesAsync();
    }

    public static async Task<int> CreateBookingAsync(
        HttpClient customer,
        int providerProfileId,
        int serviceId,
        string? notes = null)
    {
        var response = await customer.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId,
            serviceId,
            requestedDate = FutureDay(),
            notes = notes ?? "Please send a licensed plumber for a kitchen leak."
        });

        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public static async Task AcceptBookingAsync(
        HttpClient provider,
        int bookingId,
        decimal price = 90)
    {
        var response = await provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/accept",
            new
            {
                price,
                message = "I can complete this job promptly.",
                scheduledStart = SlotStart(bookingId),
                durationHours = 2
            });

        response.EnsureSuccessStatusCode();
    }

    public static ByteArrayContent PdfContent(string fileName = "certificate.pdf")
    {
        var bytes = "%PDF-1.4\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();
        return FileContent(bytes, fileName, "application/pdf");
    }

    public static ByteArrayContent JpegContent(string fileName = "portfolio.jpg")
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        return FileContent(bytes, fileName, "image/jpeg");
    }

    public static ByteArrayContent PngContent(string fileName = "license.png")
    {
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D
        };
        return FileContent(bytes, fileName, "image/png");
    }

    public static ByteArrayContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }

    public static async Task<int> UploadDocumentAsync(
        HttpClient provider,
        string documentType = "ProfessionalCertificate",
        HttpContent? file = null,
        string fileName = "certificate.pdf")
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(documentType), "documentType" }
        };
        form.Add(file ?? PdfContent(), "file", fileName);

        var response = await provider.PostAsync(
            "/api/providers/me/verification-documents",
            form);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("documents")[0].GetProperty("id").GetInt32();
    }

    public static async Task ApproveDocumentAsync(HttpClient admin, int documentId)
    {
        var response = await admin.PostAsJsonAsync(
            $"/api/admin/verification-documents/{documentId}/review",
            new { status = "Approved", note = (string?)null });
        response.EnsureSuccessStatusCode();
    }

    public static async Task OpenAllHoursAsync(
        WebApplicationFactory<Program> factory,
        int providerProfileId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await EnsureOpenHoursAsync(db, providerProfileId);
        await db.SaveChangesAsync();
    }

    public static async Task ApproveProviderViaApiAsync(HttpClient admin, int providerProfileId)
    {
        var response = await admin.PostAsJsonAsync(
            $"/api/admin/providers/{providerProfileId}/verification",
            new { status = "Approved" });
        response.EnsureSuccessStatusCode();
    }

    public static async Task<int> GetProviderProfileIdAsync(
        WebApplicationFactory<Program> factory,
        string userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ProviderProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .SingleAsync();
    }
}
