using Khidma.Api.Auth;
using Khidma.Api.Domain;
using Khidma.Api.Domain.Enums;
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

        foreach (var roleName in AppRoles.All)
        {
            await EnsureRoleAsync(roleManager, roleName);
        }

        await EnsureUserAsync(
            userManager,
            "admin@khidma.local",
            "Khidma Admin",
            AppRoles.Admin,
            adminPassword);

        var customerOne = await EnsureUserAsync(
            userManager,
            "customer@khidma.local",
            "Demo Customer",
            AppRoles.Customer,
            demoPassword);

        var customerTwo = await EnsureUserAsync(
            userManager,
            "customer2@khidma.local",
            "Nablus Customer",
            AppRoles.Customer,
            demoPassword);

        var providerOne = await EnsureUserAsync(
            userManager,
            "provider1@khidma.local",
            "Demo Provider One",
            AppRoles.Provider,
            demoPassword);

        var providerTwo = await EnsureUserAsync(
            userManager,
            "provider2@khidma.local",
            "Demo Provider Two",
            AppRoles.Provider,
            demoPassword);

        var providerThree = await EnsureUserAsync(
            userManager,
            "provider3@khidma.local",
            "Demo Provider Three",
            AppRoles.Provider,
            demoPassword);

        await EnsureCustomerProfileAsync(
            db,
            customerOne.Id,
            "Ramallah");

        await EnsureCustomerProfileAsync(
            db,
            customerTwo.Id,
            "Nablus");

        await EnsureProviderProfileAsync(
            db,
            providerOne.Id,
            "Ramallah",
            5,
            "Home services provider",
            approved: true);

        await EnsureProviderProfileAsync(
            db,
            providerTwo.Id,
            "Hebron",
            3,
            "Technology services provider",
            approved: false);

        await EnsureProviderProfileAsync(
            db,
            providerThree.Id,
            "Bethlehem",
            7,
            "Cleaning and tutoring provider",
            approved: true);

        await db.SaveChangesAsync();

        var homeServices = await EnsureCategoryAsync(db, "Home Services");
        var technology = await EnsureCategoryAsync(db, "Technology");
        var cleaning = await EnsureCategoryAsync(db, "Cleaning");
        var tutoring = await EnsureCategoryAsync(db, "Tutoring");

        var plumbing = await EnsureServiceAsync(db, homeServices.Id, "Plumbing");
        var electrical = await EnsureServiceAsync(db, homeServices.Id, "Electrical");
        var painting = await EnsureServiceAsync(db, homeServices.Id, "Painting");
        var carpentry = await EnsureServiceAsync(db, homeServices.Id, "Carpentry");

        var computerRepair = await EnsureServiceAsync(db, technology.Id, "Computer Repair");
        var phoneRepair = await EnsureServiceAsync(db, technology.Id, "Phone Repair");
        var networkSetup = await EnsureServiceAsync(db, technology.Id, "Network Setup");

        var homeCleaning = await EnsureServiceAsync(db, cleaning.Id, "Home Cleaning");
        var carpetCleaning = await EnsureServiceAsync(db, cleaning.Id, "Carpet Cleaning");
        var windowCleaning = await EnsureServiceAsync(db, cleaning.Id, "Window Cleaning");

        var mathTutoring = await EnsureServiceAsync(db, tutoring.Id, "Math Tutoring");
        var englishTutoring = await EnsureServiceAsync(db, tutoring.Id, "English Tutoring");

        var providerOneProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerOne.Id);

        var providerTwoProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerTwo.Id);

        var providerThreeProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerThree.Id);

        await EnsureProviderServiceAsync(db, providerOneProfile.Id, plumbing.Id);
        await EnsureProviderServiceAsync(db, providerOneProfile.Id, electrical.Id);
        await EnsureProviderServiceAsync(db, providerOneProfile.Id, painting.Id);
        await EnsureProviderServiceAsync(db, providerOneProfile.Id, carpentry.Id);

        await EnsureProviderServiceAsync(db, providerTwoProfile.Id, computerRepair.Id);
        await EnsureProviderServiceAsync(db, providerTwoProfile.Id, phoneRepair.Id);
        await EnsureProviderServiceAsync(db, providerTwoProfile.Id, networkSetup.Id);

        await EnsureProviderServiceAsync(db, providerThreeProfile.Id, homeCleaning.Id);
        await EnsureProviderServiceAsync(db, providerThreeProfile.Id, carpetCleaning.Id);
        await EnsureProviderServiceAsync(db, providerThreeProfile.Id, windowCleaning.Id);
        await EnsureProviderServiceAsync(db, providerThreeProfile.Id, mathTutoring.Id);
        await EnsureProviderServiceAsync(db, providerThreeProfile.Id, englishTutoring.Id);

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

    private static async Task EnsureCustomerProfileAsync(
        AppDbContext db,
        string userId,
        string city)
    {
        if (await db.CustomerProfiles.AnyAsync(p => p.UserId == userId))
        {
            return;
        }

        db.CustomerProfiles.Add(new CustomerProfile
        {
            UserId = userId,
            City = city,
            DefaultContact = null
        });
    }

    private static async Task EnsureProviderProfileAsync(
        AppDbContext db,
        string userId,
        string city,
        int yearsOfExperience,
        string bio,
        bool approved)
    {
        var status = approved
            ? ProviderVerificationStatus.Approved
            : ProviderVerificationStatus.PendingReview;

        var existing = await db.ProviderProfiles
            .SingleOrDefaultAsync(p => p.UserId == userId);

        if (existing is not null)
        {
            existing.VerificationStatus = status;
            return;
        }

        db.ProviderProfiles.Add(new ProviderProfile
        {
            UserId = userId,
            City = city,
            YearsOfExperience = yearsOfExperience,
            Bio = bio,
            VerificationStatus = status,
            AverageRating = 0,
            ReviewCount = 0
        });
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
