using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class ServiceProvidersListingTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public ServiceProvidersListingTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListsApprovedProviders_ForTheServiceAndCity()
    {
        var city = UniqueCity();
        var otherCity = UniqueCity();
        var serviceId = await TestHarness.GetServiceIdAsync(_factory);
        var otherServiceId = await TestHarness.GetServiceIdAsync(_factory, "Electrical");

        var (_, matchUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (_, otherCityUser) = await TestHarness.RegisterAsync(_factory, "Provider", otherCity);
        var (_, otherServiceUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);
        var (_, pendingUser) = await TestHarness.RegisterAsync(_factory, "Provider", city);

        await TestHarness.ApproveProviderAsync(_factory, matchUser.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, otherCityUser.Id, otherCity, serviceId);
        await TestHarness.ApproveProviderAsync(_factory, otherServiceUser.Id, city, otherServiceId);
        await TestHarness.ApproveProviderAsync(_factory, pendingUser.Id, city, serviceId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await db.ProviderProfiles.SingleAsync(p => p.UserId == pendingUser.Id);
            pending.VerificationStatus = ProviderVerificationStatus.PendingReview;
            var match = await db.ProviderProfiles.SingleAsync(p => p.UserId == matchUser.Id);
            match.AverageRating = 4.5m;
            match.ReviewCount = 3;
            await db.SaveChangesAsync();
        }

        var matchId = await TestHarness.GetProviderProfileIdAsync(_factory, matchUser.Id);
        var anonymous = TestHarness.CreateClient(_factory);
        var response = await anonymous.GetAsync(
            $"/api/catalog/services/{serviceId}/providers?city={Uri.EscapeDataString(city.ToUpperInvariant())}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ids = doc.RootElement.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(new[] { matchId }, ids);
        Assert.Equal(4.5m, doc.RootElement[0].GetProperty("averageRating").GetDecimal());
        Assert.True(doc.RootElement[0].GetProperty("isVerified").GetBoolean());
    }

    [Fact]
    public async Task UnknownService_Returns404()
    {
        var anonymous = TestHarness.CreateClient(_factory);
        var response = await anonymous.GetAsync("/api/catalog/services/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var providers = await anonymous.GetAsync("/api/catalog/services/999999/providers");
        Assert.Equal(HttpStatusCode.NotFound, providers.StatusCode);
    }

    private static string UniqueCity() => $"L{Guid.NewGuid():N}"[..10];
}
