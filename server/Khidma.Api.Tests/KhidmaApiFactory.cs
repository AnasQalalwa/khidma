using Khidma.Api.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Khidma.Api.Tests;

public sealed class KhidmaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public KhidmaApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(TestConfiguration());
        });

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(TestConfiguration());
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                var created = roleManager.CreateAsync(new IdentityRole(roleName))
                    .GetAwaiter()
                    .GetResult();

                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to seed test roles: " +
                        string.Join("; ", created.Errors.Select(e => e.Description)));
                }
            }
        }

        if (!db.Categories.Any())
        {
            db.Categories.Add(new Category { Name = "Home Services" });
            db.SaveChanges();
            var category = db.Categories.Single();
            db.Services.Add(new Service
            {
                Name = "Plumbing",
                CategoryId = category.Id
            });
            db.SaveChanges();
        }

        return host;
    }

    private static Dictionary<string, string?> TestConfiguration() => new()
    {
        ["ConnectionStrings:Default"] =
            "Server=(localdb)\\mssqllocaldb;Database=Khidma_Ignored;Trusted_Connection=True;TrustServerCertificate=True",
        ["Seed:AdminPassword"] = "Test_Admin_123!",
        ["Seed:DemoPassword"] = "Test_Demo_123!"
    };
}
