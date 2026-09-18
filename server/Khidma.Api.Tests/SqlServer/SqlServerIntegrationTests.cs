using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
using Khidma.Api.Services.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests.SqlServer;

public sealed class SqlServerIntegrationTests : IAsyncLifetime
{
    private SqlServerApiFactory? _factory;

    public Task InitializeAsync()
    {
        if (!SqlServerTestSettings.IsEnabled)
        {
            return Task.CompletedTask;
        }

        _factory = new SqlServerApiFactory();
        _ = _factory.Services;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [SqlServerFact]
    public async Task ConcurrentAccept_ProducesOneBooking_And409ForLoser()
    {
        var factory = RequireFactory();
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(factory, "Customer", city);
        var (providerOne, userOne) = await TestHarness.RegisterAsync(factory, "Provider", city);
        var (providerTwo, userTwo) = await TestHarness.RegisterAsync(factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(factory);
        await TestHarness.ApproveProviderAsync(factory, userOne.Id, city, serviceId);
        await TestHarness.ApproveProviderAsync(factory, userTwo.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        var offerOne = await TestHarness.SubmitOfferAsync(providerOne, requestId, 90);
        var offerTwo = await TestHarness.SubmitOfferAsync(providerTwo, requestId, 80);

        var first = customer.PostAsync($"/api/offers/{offerOne}/accept", null);
        var second = customer.PostAsync($"/api/offers/{offerOne}/accept", null);
        await Task.WhenAll(first, second);

        var statuses = new[] { first.Result.StatusCode, second.Result.StatusCode }
            .OrderBy(code => code)
            .ToArray();
        Assert.Equal(HttpStatusCode.OK, statuses[0]);
        Assert.Equal(HttpStatusCode.Conflict, statuses[1]);

        var loser = first.Result.StatusCode == HttpStatusCode.Conflict
            ? first.Result
            : second.Result;
        var loserBody = await loser.Content.ReadAsStringAsync();
        Assert.Contains(
            OfferService.AnotherOfferAcceptedFirst,
            loserBody,
            StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookings = await db.Bookings
            .Where(b => b.ServiceRequestId == requestId)
            .ToListAsync();
        Assert.Single(bookings);

        var accepted = await db.Offers.SingleAsync(o => o.Id == offerOne);
        var sibling = await db.Offers.SingleAsync(o => o.Id == offerTwo);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
        Assert.Equal(OfferStatus.Rejected, sibling.Status);
    }

    [SqlServerFact]
    public async Task DuplicateNonWithdrawnOffer_IsRejectedByFilteredIndex()
    {
        var factory = RequireFactory();
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(factory);
        await TestHarness.ApproveProviderAsync(factory, providerUser.Id, city, serviceId);
        var requestId = await TestHarness.CreateRequestAsync(customer, serviceId, city);
        await TestHarness.SubmitOfferAsync(provider, requestId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Offers.Add(new Offer
        {
            ServiceRequestId = requestId,
            ProviderId = providerUser.Id,
            Price = 70,
            Message = "Duplicate non-withdrawn offer.",
            EstimatedDate = TestHarness.FutureDate(),
            Status = OfferStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private SqlServerApiFactory RequireFactory()
    {
        Assert.NotNull(_factory);
        return _factory;
    }

    private static string UniqueCity() => $"S{Guid.NewGuid():N}"[..10];
}
