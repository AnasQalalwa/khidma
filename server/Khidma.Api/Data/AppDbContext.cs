using Khidma.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
