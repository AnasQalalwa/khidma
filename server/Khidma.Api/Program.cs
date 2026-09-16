using Khidma.Api.Auth;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Khidma.Api.Infrastructure;
using Khidma.Api.Services.Audit;
using Khidma.Api.Services.Documents;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString =
        builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "Connection string 'Default' was not found.");

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
        CookieAuthProblemWriter.WriteAsync(
            context,
            StatusCodes.Status401Unauthorized,
            "Unauthorized");

    options.Events.OnRedirectToAccessDenied = context =>
        CookieAuthProblemWriter.WriteAsync(
            context,
            StatusCodes.Status403Forbidden,
            "Forbidden");
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = DocumentFileValidator.MaxFileSizeBytes + 256 * 1024;
});
builder.Services.Configure<ProviderDocumentStorageOptions>(
    builder.Configuration.GetSection("ProviderDocuments"));
builder.Services.AddSingleton<IProviderDocumentStorage, LocalProviderDocumentStorage>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<UserRegistrationService>();
builder.Services.AddScoped<Khidma.Api.Services.ServiceRequests.IServiceRequestService, Khidma.Api.Services.ServiceRequests.ServiceRequestService>();
builder.Services.AddScoped<Khidma.Api.Services.Offers.IOfferService, Khidma.Api.Services.Offers.OfferService>();
builder.Services.AddScoped<Khidma.Api.Services.Bookings.IBookingService, Khidma.Api.Services.Bookings.BookingService>();
builder.Services.AddScoped<Khidma.Api.Services.Reviews.IReviewService, Khidma.Api.Services.Reviews.ReviewService>();
builder.Services.AddScoped<Khidma.Api.Services.Providers.IProviderProfileService, Khidma.Api.Services.Providers.ProviderProfileService>();
builder.Services.AddScoped<Khidma.Api.Services.Admin.IAdminService, Khidma.Api.Services.Admin.AdminService>();
builder.Services.AddScoped<Khidma.Api.Services.Dashboard.IDashboardService, Khidma.Api.Services.Dashboard.DashboardService>();
builder.Services.AddScoped<Khidma.Api.Services.Verification.IProviderVerificationService, Khidma.Api.Services.Verification.ProviderVerificationService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(app.Services, app.Configuration);
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallback("/api/{**slug}", () => Results.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Not Found"));

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
