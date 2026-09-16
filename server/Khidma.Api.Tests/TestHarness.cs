using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Auth;
using Khidma.Api.Contracts.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
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

    public static HttpClient CreateClient(KhidmaApiFactory factory)
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

    public static async Task<(HttpClient Client, CurrentUserDto User)> RegisterAsync(
        KhidmaApiFactory factory,
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
        KhidmaApiFactory factory)
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
        KhidmaApiFactory factory,
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
        KhidmaApiFactory factory,
        string userId,
        string? city = null,
        params int[] serviceIds)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.ProviderProfiles
            .Include(p => p.ProviderServices)
            .SingleAsync(p => p.UserId == userId);

        profile.IsApproved = true;
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

        await db.SaveChangesAsync();
    }

    public static async Task SetApprovedAsync(
        KhidmaApiFactory factory,
        string userId,
        bool isApproved)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.ProviderProfiles.SingleAsync(p => p.UserId == userId);
        profile.IsApproved = isApproved;
        await db.SaveChangesAsync();
    }

    public static async Task<int> CreateRequestAsync(
        HttpClient customer,
        int serviceId,
        string city,
        string title = "Fix a leaking tap")
    {
        var response = await customer.PostAsJsonAsync("/api/service-requests", new
        {
            serviceId,
            title,
            description = "Please send a licensed plumber for a kitchen leak.",
            city,
            preferredDate = FutureDate(),
            budgetMin = 50,
            budgetMax = 120
        });

        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public static async Task<int> SubmitOfferAsync(
        HttpClient provider,
        int requestId,
        decimal price = 90)
    {
        var response = await provider.PostAsJsonAsync(
            $"/api/service-requests/{requestId}/offers",
            new
            {
                price,
                message = "I can complete this job promptly.",
                estimatedDate = FutureDate()
            });

        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }
}
