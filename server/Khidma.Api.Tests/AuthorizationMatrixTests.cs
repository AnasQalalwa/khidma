using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class AuthorizationMatrixTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public AuthorizationMatrixTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_PostBooking_Returns401()
    {
        var client = TestHarness.CreateClient(_factory);
        await AntiforgeryTestHelper.AttachTokenAsync(client);
        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = 1,
            serviceId = 1,
            requestedDate = TestHarness.FutureDay()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Provider_CannotCreateBooking_Returns403()
    {
        var (provider, _) = await TestHarness.RegisterAsync(_factory, "Provider", "Ramallah");
        var response = await provider.PostAsJsonAsync("/api/bookings", new
        {
            providerProfileId = 1,
            serviceId = 1,
            requestedDate = TestHarness.FutureDay()
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotAcceptBooking_Returns403()
    {
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", "Ramallah");
        var response = await customer.PostAsJsonAsync("/api/bookings/1/accept", new
        {
            price = 50
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProviderB_CannotAcceptProviderABooking()
    {
        var city = $"X{Guid.NewGuid():N}"[..10];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (_, userA) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (providerB, userB) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        await TestHarness.ApproveProviderAsync(_factory, userA.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, userB.Id, city, serviceId);
        var profileA = await TestHarness.GetProviderProfileIdAsync(_factory, userA.Id);
        var bookingId = await TestHarness.CreateBookingAsync(customer, profileA, serviceId);

        var response = await providerB.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/accept",
            new { price = 80 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed class PaginationTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public PaginationTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListEndpoints_HonorPageSizeCapAndPaging()
    {
        var city = $"P{Guid.NewGuid():N}"[..10];
        var (customer, _) = await TestHarness.RegisterAsync(_factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        int[] serviceIds;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Khidma.Api.Data.AppDbContext>();
            var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
            db.Services.Add(new Khidma.Api.Domain.Service
            {
                Name = $"Paging {Guid.NewGuid():N}",
                CategoryId = categoryId
            });
            await db.SaveChangesAsync();
            serviceIds = await db.Services.OrderBy(s => s.Id).Select(s => s.Id).Take(3).ToArrayAsync();
        }

        await TestHarness.ApproveProviderAsync(_factory, providerUser.Id, city, serviceIds);
        var profileId = await TestHarness.GetProviderProfileIdAsync(_factory, providerUser.Id);
        foreach (var serviceId in serviceIds)
        {
            await TestHarness.CreateBookingAsync(customer, profileId, serviceId);
        }

        var page = await customer.GetAsync("/api/bookings/mine?page=1&pageSize=2");
        page.EnsureSuccessStatusCode();
        using var doc = System.Text.Json.JsonDocument.Parse(await page.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("items").GetArrayLength());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 3);

        var capped = await customer.GetAsync("/api/bookings/mine?page=1&pageSize=99");
        capped.EnsureSuccessStatusCode();
        using var cappedDoc = System.Text.Json.JsonDocument.Parse(await capped.Content.ReadAsStringAsync());
        Assert.Equal(50, cappedDoc.RootElement.GetProperty("pageSize").GetInt32());
        Assert.NotNull(provider);
    }
}
