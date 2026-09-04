using Khidma.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var adminPassword = configuration["Seed:AdminPassword"]
            ?? throw new InvalidOperationException(
                "Seed:AdminPassword is missing from configuration.");

        var demoPassword = configuration["Seed:DemoPassword"]
            ?? throw new InvalidOperationException(
                "Seed:DemoPassword is missing from configuration.");

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Customer");
        await EnsureRoleAsync(roleManager, "Provider");

        var admin = await EnsureUserAsync(
            userManager,
            "admin@khidma.local",
            "Khidma Admin",
            "Admin",
            adminPassword);

        var customer = await EnsureUserAsync(
            userManager,
            "customer@khidma.local",
            "Demo Customer",
            "Customer",
            demoPassword);

        var providerOne = await EnsureUserAsync(
            userManager,
            "provider1@khidma.local",
            "Demo Provider One",
            "Provider",
            demoPassword);

        var providerTwo = await EnsureUserAsync(
            userManager,
            "provider2@khidma.local",
            "Demo Provider Two",
            "Provider",
            demoPassword);

        if (!await db.CustomerProfiles.AnyAsync(
                p => p.UserId == customer.Id))
        {
            db.CustomerProfiles.Add(new CustomerProfile
            {
                UserId = customer.Id,
                City = "Ramallah",
                DefaultContact = "0590000000"
            });
        }

        if (!await db.ProviderProfiles.AnyAsync(
                p => p.UserId == providerOne.Id))
        {
            db.ProviderProfiles.Add(new ProviderProfile
            {
                UserId = providerOne.Id,
                City = "Ramallah",
                YearsOfExperience = 5,
                Bio = "Home services provider",
                IsApproved = true,
                AverageRating = 0,
                ReviewCount = 0
            });
        }

        if (!await db.ProviderProfiles.AnyAsync(
                p => p.UserId == providerTwo.Id))
        {
            db.ProviderProfiles.Add(new ProviderProfile
            {
                UserId = providerTwo.Id,
                City = "Ramallah",
                YearsOfExperience = 3,
                Bio = "Technology services provider",
                IsApproved = true,
                AverageRating = 0,
                ReviewCount = 0
            });
        }

        await db.SaveChangesAsync();

        var homeServices = await EnsureCategoryAsync(
            db,
            "Home Services");

        var technology = await EnsureCategoryAsync(
            db,
            "Technology");

        var plumbing = await EnsureServiceAsync(
            db,
            homeServices.Id,
            "Plumbing");

        var electrical = await EnsureServiceAsync(
            db,
            homeServices.Id,
            "Electrical");

        var computerRepair = await EnsureServiceAsync(
            db,
            technology.Id,
            "Computer Repair");

        var providerOneProfile =
            await db.ProviderProfiles.SingleAsync(
                p => p.UserId == providerOne.Id);

        var providerTwoProfile =
            await db.ProviderProfiles.SingleAsync(
                p => p.UserId == providerTwo.Id);

        await EnsureProviderServiceAsync(
            db,
            providerOneProfile.Id,
            plumbing.Id);

        await EnsureProviderServiceAsync(
            db,
            providerOneProfile.Id,
            electrical.Id);

        await EnsureProviderServiceAsync(
            db,
            providerTwoProfile.Id,
            computerRepair.Id);

        await db.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(
            new IdentityRole(roleName));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create role '{roleName}': " +
                string.Join(
                    "; ",
                    result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string role,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create user '{email}': " +
                    string.Join(
                        "; ",
                        createResult.Errors.Select(
                            e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not add '{email}' to role '{role}': " +
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }
        }

        return user;
    }

    private static async Task<Category> EnsureCategoryAsync(
        AppDbContext db,
        string name)
    {
        var category =
            await db.Categories.SingleOrDefaultAsync(
                c => c.Name == name);

        if (category is not null)
        {
            return category;
        }

        category = new Category
        {
            Name = name
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category;
    }

    private static async Task<Khidma.Api.Domain.Service>
        EnsureServiceAsync(
            AppDbContext db,
            int categoryId,
            string name)
    {
        var service =
            await db.Services.SingleOrDefaultAsync(
                s => s.CategoryId == categoryId &&
                     s.Name == name);

        if (service is not null)
        {
            return service;
        }

        service = new Khidma.Api.Domain.Service
        {
            CategoryId = categoryId,
            Name = name
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        return service;
    }

    private static async Task EnsureProviderServiceAsync(
        AppDbContext db,
        int providerProfileId,
        int serviceId)
    {
        var exists =
            await db.ProviderServices.AnyAsync(
                ps =>
                    ps.ProviderProfileId == providerProfileId &&
                    ps.ServiceId == serviceId);

        if (exists)
        {
            return;
        }

        db.ProviderServices.Add(new ProviderService
        {
            ProviderProfileId = providerProfileId,
            ServiceId = serviceId
        });
    }
}
