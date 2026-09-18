using Khidma.Api.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Khidma.Api.Tests;

public sealed class KhidmaApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly string _documentRoot =
        Path.Combine(Path.GetTempPath(), $"khidma-docs-{Guid.NewGuid():N}");
    private readonly string _webRoot =
        Path.Combine(Path.GetTempPath(), $"khidma-www-{Guid.NewGuid():N}");

    public KhidmaApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        _connection.Open();
        Directory.CreateDirectory(_documentRoot);
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(
            Path.Combine(_webRoot, "index.html"),
            """<!doctype html><html><body><div id="root">Khidma SPA</div></body></html>""");
    }

    public string DocumentRoot => _documentRoot;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseWebRoot(_webRoot);

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
                options.UseSqlite(_connection);
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            TryDeleteDocuments();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
        TryDeleteDocuments();
    }

    private void TryDeleteDocuments()
    {
        try
        {
            if (Directory.Exists(_documentRoot))
            {
                Directory.Delete(_documentRoot, recursive: true);
            }

            if (Directory.Exists(_webRoot))
            {
                Directory.Delete(_webRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"Could not delete test documents at {_documentRoot}.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not delete test documents at {_documentRoot}.");
        }
    }

    private Dictionary<string, string?> TestConfiguration() => new()
    {
        ["Seed:AdminPassword"] = "Test_Admin_123!",
        ["Seed:DemoPassword"] = "Test_Demo_123!",
        ["ProviderDocuments:RootPath"] = _documentRoot
    };
}
