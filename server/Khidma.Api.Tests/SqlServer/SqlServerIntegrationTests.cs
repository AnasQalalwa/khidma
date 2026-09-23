using System.Net;
using System.Net.Http.Json;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
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
    public async Task ConcurrentAccept_OneSucceeds_AndLoserConflicts()
    {
        var factory = RequireFactory();
        var city = UniqueCity();
        var (customer, _) = await TestHarness.RegisterAsync(factory, "Customer", city);
        var (provider, providerUser) = await TestHarness.RegisterAsync(factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(factory);
        await TestHarness.ApproveProviderAsync(factory, providerUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(factory, providerUser.Id);
        var bookingId = await TestHarness.CreateBookingAsync(customer, profileId, serviceId);

        var first = provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/accept",
            new { price = 90, message = "First", scheduledStart = TestHarness.SlotStart(bookingId), durationHours = 2 });
        var second = provider.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/accept",
            new { price = 80, message = "Second", scheduledStart = TestHarness.SlotStart(bookingId), durationHours = 2 });
        await Task.WhenAll(first, second);

        var statuses = new[] { first.Result.StatusCode, second.Result.StatusCode }
            .OrderBy(code => code)
            .ToArray();
        Assert.Equal(HttpStatusCode.OK, statuses[0]);
        Assert.Equal(HttpStatusCode.Conflict, statuses[1]);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Scheduled, booking.Status);
        Assert.NotNull(booking.QuotedPrice);
    }

    [SqlServerFact]
    public async Task DuplicatePendingBooking_IsRejectedByFilteredIndex()
    {
        var factory = RequireFactory();
        var city = UniqueCity();
        var (customer, customerUser) = await TestHarness.RegisterAsync(factory, "Customer", city);
        var (_, providerUser) = await TestHarness.RegisterAsync(factory, "Provider", city);
        var serviceId = await TestHarness.GetServiceIdAsync(factory);
        await TestHarness.ApproveProviderAsync(factory, providerUser.Id, city, serviceId);
        var profileId = await TestHarness.GetProviderProfileIdAsync(factory, providerUser.Id);
        await TestHarness.CreateBookingAsync(customer, profileId, serviceId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Bookings.Add(new Booking
        {
            CustomerId = customerUser.Id,
            ProviderId = providerUser.Id,
            ServiceId = serviceId,
            City = city,
            RequestedDate = DateOnly.Parse(TestHarness.FutureDay()),
            Status = BookingStatus.Pending,
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
