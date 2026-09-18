using Khidma.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Khidma.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ProviderVerificationDocument> ProviderVerificationDocuments =>
        Set<ProviderVerificationDocument>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        if (IsSqliteProvider())
        {
            ApplySqliteTestCompatibility(builder);
        }
    }

    private bool IsSqliteProvider() =>
        string.Equals(
            Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);

    /// <summary>
    /// SQLite is used only by automated tests. Production remains SQL Server.
    /// This shim maps SQL Server rowversion to a BLOB concurrency token so
    /// EnsureCreated can succeed without changing production configuration.
    /// </summary>
    private static void ApplySqliteTestCompatibility(ModelBuilder builder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks,
            value => new DateTimeOffset(value, TimeSpan.Zero));
        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, long?>(
            value => value.HasValue ? value.Value.UtcTicks : null,
            value => value.HasValue
                ? new DateTimeOffset(value.Value, TimeSpan.Zero)
                : null);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(byte[]) &&
                    property.IsConcurrencyToken &&
                    property.ValueGenerated == ValueGenerated.OnAddOrUpdate)
                {
                    property.SetColumnType("BLOB");
                    property.ValueGenerated = ValueGenerated.Never;
                    property.IsNullable = true;
                }

                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                    property.SetColumnType("INTEGER");
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableDateTimeOffsetConverter);
                    property.SetColumnType("INTEGER");
                }
            }

            foreach (var index in entityType.GetIndexes())
            {
                var filter = index.GetFilter();
                if (!string.IsNullOrEmpty(filter))
                {
                    index.SetFilter(filter.Replace("[", "\"", StringComparison.Ordinal)
                        .Replace("]", "\"", StringComparison.Ordinal));
                }
            }
        }
    }
}
