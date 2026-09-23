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
            adminPassword,
            "+970 0590000001");

        var customerOne = await EnsureUserAsync(
            userManager,
            "customer@khidma.local",
            "Demo Customer",
            AppRoles.Customer,
            demoPassword,
            "+970 0591111111");

        var customerTwo = await EnsureUserAsync(
            userManager,
            "customer2@khidma.local",
            "Nablus Customer",
            AppRoles.Customer,
            demoPassword,
            "+970 0592222222");

        var providerOne = await EnsureUserAsync(
            userManager,
            "provider1@khidma.local",
            "Demo Provider One",
            AppRoles.Provider,
            demoPassword,
            "+970 0593333333");

        var providerTwo = await EnsureUserAsync(
            userManager,
            "provider2@khidma.local",
            "Demo Provider Two",
            AppRoles.Provider,
            demoPassword,
            "+970 0594444444");

        var providerThree = await EnsureUserAsync(
            userManager,
            "provider3@khidma.local",
            "Demo Provider Three",
            AppRoles.Provider,
            demoPassword,
            "+970 0595555555");

        var providerFour = await EnsureUserAsync(
            userManager,
            "provider4@khidma.local",
            "Demo Provider Four",
            AppRoles.Provider,
            demoPassword,
            "+970 0596666666");

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

        await EnsureProviderProfileAsync(
            db,
            providerFour.Id,
            "Ramallah",
            4,
            "Plumbing provider",
            approved: true);

        await db.SaveChangesAsync();

        var homeServices = await EnsureCategoryAsync(db, "Home Services", "Repairs, installs and more.");
        var technology = await EnsureCategoryAsync(db, "Technology", "Tech help made easy.");
        var cleaning = await EnsureCategoryAsync(db, "Cleaning", "A cleaner, healthier home.");
        var tutoring = await EnsureCategoryAsync(db, "Tutoring", "Learn with expert tutors.");

        var plumbing = await EnsureServiceAsync(db, homeServices.Id, "Plumbing", "Fix leaks, installations and more.");
        var electrical = await EnsureServiceAsync(db, homeServices.Id, "Electrical", "Safe and reliable electrical services for your home.");
        var painting = await EnsureServiceAsync(db, homeServices.Id, "Painting", "Give your space a fresh new look.");
        var carpentry = await EnsureServiceAsync(db, homeServices.Id, "Carpentry", "Custom woodwork, repairs and installations.");

        var computerRepair = await EnsureServiceAsync(db, technology.Id, "Computer Repair", "Fast and reliable computer repair services.");
        var phoneRepair = await EnsureServiceAsync(db, technology.Id, "Phone Repair", "Screen repair, battery replacement and more.");
        var networkSetup = await EnsureServiceAsync(db, technology.Id, "Network Setup", "Set up and optimize your home or office network.");

        var homeCleaning = await EnsureServiceAsync(db, cleaning.Id, "Home Cleaning", "Reliable home cleaning services for a cleaner space.");
        var carpetCleaning = await EnsureServiceAsync(db, cleaning.Id, "Carpet Cleaning", "Professional carpet cleaning for a fresher, healthier home.");
        var windowCleaning = await EnsureServiceAsync(db, cleaning.Id, "Window Cleaning", "Crystal-clear windows inside and out.");

        var mathTutoring = await EnsureServiceAsync(db, tutoring.Id, "Math Tutoring", "Get help with math from qualified tutors.");
        var englishTutoring = await EnsureServiceAsync(db, tutoring.Id, "English Tutoring", "Improve your English with experienced tutors.");

        var providerOneProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerOne.Id);

        var providerTwoProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerTwo.Id);

        var providerThreeProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerThree.Id);

        var providerFourProfile =
            await db.ProviderProfiles.SingleAsync(p => p.UserId == providerFour.Id);

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

        await EnsureProviderServiceAsync(db, providerFourProfile.Id, plumbing.Id);

        await db.SaveChangesAsync();

        await EnsureWorkingHoursAsync(db);

        await EnsureDemoBookingsAsync(
            db,
            customerOne,
            providerOne,
            providerFour,
            plumbing,
            electrical);
        await EnsureLegacySlotsAsync(db);
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
        string password,
        string phoneNumber)
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
                PhoneNumber = phoneNumber,
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
        else if (string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                 !user.PhoneNumber.StartsWith("+970 ", StringComparison.Ordinal))
        {
            user.PhoneNumber = phoneNumber;
            var phoneResult = await userManager.UpdateAsync(user);
            if (!phoneResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not set phone for '{email}': " +
                    string.Join(
                        "; ",
                        phoneResult.Errors.Select(e => e.Description)));
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
            City = city
        });
    }

    private static async Task EnsureDemoBookingsAsync(
        AppDbContext db,
        ApplicationUser customer,
        ApplicationUser providerOne,
        ApplicationUser providerFour,
        Domain.Service plumbing,
        Domain.Service electrical)
    {
        if (await db.Bookings.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var pending = new Booking
        {
            CustomerId = customer.Id,
            ProviderId = providerOne.Id,
            ServiceId = plumbing.Id,
            City = "Ramallah",
            Notes = "Kitchen sink is leaking under the cabinet.",
            RequestedDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(3)),
            Status = BookingStatus.Pending,
            CreatedAt = now.AddHours(-2)
        };
        var scheduled = new Booking
        {
            CustomerId = customer.Id,
            ProviderId = providerFour.Id,
            ServiceId = plumbing.Id,
            City = "Ramallah",
            Notes = "Replace the bathroom faucet.",
            RequestedDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(2)),
            ScheduledStart = now.AddDays(2),
            DurationHours = 3,
            Status = BookingStatus.Scheduled,
            QuotedPrice = 180,
            ProviderMessage = "I can bring the parts and finish the same visit.",
            CreatedAt = now.AddDays(-1),
            RespondedAt = now.AddHours(-20)
        };
        var completed = new Booking
        {
            CustomerId = customer.Id,
            ProviderId = providerOne.Id,
            ServiceId = electrical.Id,
            City = "Ramallah",
            Notes = "Living room outlet stopped working.",
            RequestedDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-4)),
            ScheduledStart = now.AddDays(-4),
            DurationHours = 2,
            Status = BookingStatus.Completed,
            QuotedPrice = 120,
            ProviderMessage = "I will check the breaker and the outlet.",
            CreatedAt = now.AddDays(-6),
            RespondedAt = now.AddDays(-5),
            StartedAt = now.AddDays(-4),
            CompletedAt = now.AddDays(-4).AddHours(2)
        };

        db.Bookings.AddRange(pending, scheduled, completed);
        await db.SaveChangesAsync();

        db.Reviews.Add(new Review
        {
            BookingId = completed.Id,
            CustomerId = customer.Id,
            ProviderId = providerOne.Id,
            Rating = 5,
            Comment = "Arrived on time and fixed the outlet the same day.",
            CreatedAt = now.AddDays(-3)
        });

        var profile = await db.ProviderProfiles.SingleAsync(p => p.UserId == providerOne.Id);
        profile.AverageRating = 5;
        profile.ReviewCount = 1;
        await db.SaveChangesAsync();
    }

    private static async Task EnsureWorkingHoursAsync(AppDbContext db)
    {
        var profileIds = await db.ProviderProfiles.Select(p => p.Id).ToListAsync();
        var withHours = await db.ProviderWorkingHours
            .Select(h => h.ProviderProfileId)
            .Distinct()
            .ToListAsync();
        foreach (var profileId in profileIds.Except(withHours))
        {
            for (var day = 0; day <= 4; day++)
            {
                for (var hour = 8; hour <= 15; hour++)
                {
                    db.ProviderWorkingHours.Add(new ProviderWorkingHour
                    {
                        ProviderProfileId = profileId,
                        DayOfWeek = day,
                        Hour = hour
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureLegacySlotsAsync(AppDbContext db)
    {
        var missing = await db.Bookings
            .Where(b => b.ScheduledStart == null &&
                (b.Status == BookingStatus.Scheduled ||
                 b.Status == BookingStatus.InProgress ||
                 b.Status == BookingStatus.Completed))
            .ToListAsync();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var booking in missing)
        {
            var morning = booking.RequestedDate.ToDateTime(new TimeOnly(9, 0));
            booking.ScheduledStart = new DateTimeOffset(morning, TimeSpan.FromHours(3));
            booking.DurationHours ??= booking.Status == BookingStatus.Completed ? 2 : 3;
        }

        await db.SaveChangesAsync();
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
        string name,
        string description)
    {
        var category =
            await db.Categories.SingleOrDefaultAsync(
                c => c.Name == name);

        if (category is not null)
        {
            if (string.IsNullOrWhiteSpace(category.Description))
            {
                category.Description = description;
                await db.SaveChangesAsync();
            }

            return category;
        }

        category = new Category
        {
            Name = name,
            Description = description
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category;
    }

    private static async Task<Khidma.Api.Domain.Service>
        EnsureServiceAsync(
            AppDbContext db,
            int categoryId,
            string name,
            string description)
    {
        var service =
            await db.Services.SingleOrDefaultAsync(
                s => s.CategoryId == categoryId &&
                     s.Name == name);

        if (service is not null)
        {
            if (string.IsNullOrWhiteSpace(service.Description))
            {
                service.Description = description;
                await db.SaveChangesAsync();
            }

            return service;
        }

        service = new Khidma.Api.Domain.Service
        {
            CategoryId = categoryId,
            Name = name,
            Description = description
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
