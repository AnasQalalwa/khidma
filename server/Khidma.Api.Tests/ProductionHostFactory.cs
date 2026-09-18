using Khidma.Api.Auth;
using Khidma.Api.Contracts.Catalog;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Services;
using Khidma.Api.Services.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Khidma.Api.Tests;

public sealed class ProductionHostFactory : WebApplicationFactory<Program>
{
    public const string ExceptionMarker = "SECRET_STACK_TRACE_LEAK";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly string _documentRoot =
        Path.Combine(Path.GetTempPath(), $"khidma-prod-docs-{Guid.NewGuid():N}");
    private readonly string _webRoot =
        Path.Combine(Path.GetTempPath(), $"khidma-prod-www-{Guid.NewGuid():N}");

    public ProductionHostFactory()
    {
        _connection.Open();
        Directory.CreateDirectory(_documentRoot);
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(
            Path.Combine(_webRoot, "index.html"),
            """<!doctype html><html><body><div id="root">Khidma SPA</div></body></html>""");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseWebRoot(_webRoot);
        builder.UseSetting("Tests:SkipConfiguredSqlServer", "true");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(Configuration());
        });

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(ICatalogService))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
            services.AddScoped<ICatalogService, ThrowingCatalogService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(Configuration());
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in AppRoles.All)
        {
            if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
            }
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            TryDeleteScratch();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
        TryDeleteScratch();
    }

    private void TryDeleteScratch()
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
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Could not delete production-host scratch files: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Could not delete production-host scratch files: {ex.Message}");
        }
    }

    private Dictionary<string, string?> Configuration() => new()
    {
        ["ConnectionStrings:Default"] =
            "Server=(localdb)\\mssqllocaldb;Database=Khidma_Unused;Trusted_Connection=True;TrustServerCertificate=True",
        ["Seed:AdminPassword"] = "Test_Admin_123!",
        ["Seed:DemoPassword"] = "Test_Demo_123!",
        ["ProviderDocuments:RootPath"] = _documentRoot,
        ["Tests:SkipConfiguredSqlServer"] = "true"
    };

    private sealed class ThrowingCatalogService : ICatalogService
    {
        public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(ExceptionMarker);

        public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(ExceptionMarker);

        public Task<ServiceResult<IReadOnlyList<ServiceDto>>> GetServicesByCategoryAsync(
            int categoryId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(ExceptionMarker);
    }
}
