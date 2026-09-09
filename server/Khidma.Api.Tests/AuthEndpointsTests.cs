using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Contracts.Auth;
using Khidma.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Khidma.Api.Domain;

namespace Khidma.Api.Tests;

public sealed class AuthEndpointsTests : IClassFixture<KhidmaApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly KhidmaApiFactory _factory;

    public AuthEndpointsTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousMe_Returns401Json_NotHtmlOrRedirect()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.NotEqual("text/html", contentType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unauthorized", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_Customer_ReturnsCurrentUser()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var email = UniqueEmail("customer");
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Customer",
            email,
            password = "ValidPass1!",
            role = "Customer",
            city = "Ramallah"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal("Customer", user.Role);
        Assert.Equal("Test Customer", user.FullName);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Register_Provider_ReturnsCurrentUser()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var email = UniqueEmail("provider");
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Provider",
            email,
            password = "ValidPass1!",
            role = "Provider",
            city = "Nablus",
            yearsOfExperience = 4,
            bio = "Reliable technician"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("Provider", user.Role);
    }

    [Fact]
    public async Task Register_Customer_PersistsUserRoleAndProfile()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var email = UniqueEmail("persist-customer");
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Persist Customer",
            email,
            password = "ValidPass1!",
            role = "Customer",
            city = "Ramallah"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var persisted = await userManager.FindByEmailAsync(email);
        Assert.NotNull(persisted);
        Assert.True(await userManager.IsInRoleAsync(persisted, "Customer"));
        Assert.True(await db.CustomerProfiles.AnyAsync(p => p.UserId == persisted.Id));
        Assert.False(await db.ProviderProfiles.AnyAsync(p => p.UserId == persisted.Id));
    }

    [Fact]
    public async Task Register_Provider_PersistsUserRoleAndProfile()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var email = UniqueEmail("persist-provider");
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Persist Provider",
            email,
            password = "ValidPass1!",
            role = "Provider",
            city = "Hebron",
            yearsOfExperience = 2
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var persisted = await userManager.FindByEmailAsync(email);
        Assert.NotNull(persisted);
        Assert.True(await userManager.IsInRoleAsync(persisted, "Provider"));
        Assert.True(await db.ProviderProfiles.AnyAsync(p => p.UserId == persisted.Id));
        Assert.False(await db.CustomerProfiles.AnyAsync(p => p.UserId == persisted.Id));
    }

    [Fact]
    public async Task Register_Admin_IsRejected()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Hacker",
            email = UniqueEmail("admin"),
            password = "ValidPass1!",
            role = "Admin",
            city = "Ramallah"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Admin", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "missing@khidma.local",
            password = "WrongPass1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("missing@khidma.local", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ValidCustomer_ThenLogout_ClearsSession()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var email = UniqueEmail("login");
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login Customer",
            email,
            password = "ValidPass1!",
            role = "Customer",
            city = "Bethlehem"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var anonymousMe = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousMe.StatusCode);

        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "ValidPass1!"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var authenticatedMe = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, authenticatedMe.StatusCode);
    }

    [Fact]
    public async Task PostWithoutAntiforgery_IsRejected()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "someone@khidma.local",
            password = "ValidPass1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWithAntiforgery_IsAccepted()
    {
        var client = CreateClient();
        await AntiforgeryTestHelper.AttachTokenAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "missing@khidma.local",
            password = "ValidPass1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@khidma.test";
}
