using Khidma.Api.Data;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Khidma.Api.Tests;

public sealed class RelationalConstraintTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public RelationalConstraintTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UniqueCustomerProfileUserId_RejectsDuplicates()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"constraint-{Guid.NewGuid():N}@khidma.test";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Constraint Customer",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await userManager.CreateAsync(user, "ValidPass1!");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        db.CustomerProfiles.Add(new CustomerProfile
        {
            UserId = user.Id,
            City = "Ramallah"
        });
        await db.SaveChangesAsync();

        using var duplicateScope = _factory.Services.CreateScope();
        var duplicateDb = duplicateScope.ServiceProvider.GetRequiredService<AppDbContext>();
        duplicateDb.CustomerProfiles.Add(new CustomerProfile
        {
            UserId = user.Id,
            City = "Nablus"
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateDb.SaveChangesAsync());
    }
}
