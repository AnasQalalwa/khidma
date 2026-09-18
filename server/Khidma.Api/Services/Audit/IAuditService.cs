namespace Khidma.Api.Services.Audit;

public static class AuditCategories
{
    public const string Auth = "Auth";
    public const string Provider = "Provider";
    public const string Admin = "Admin";
    public const string Request = "Request";
    public const string Offer = "Offer";
    public const string Booking = "Booking";
    public const string Review = "Review";
}

public static class AuditOutcomes
{
    public const string Success = "Success";
    public const string Denied = "Denied";
    public const string Failed = "Failed";
}

public static class AuditActions
{
    public const string RegisterSucceeded = "Auth.RegisterSucceeded";
    public const string LoginSucceeded = "Auth.LoginSucceeded";
    public const string LoginFailed = "Auth.LoginFailed";
    public const string Logout = "Auth.Logout";

    public const string ProviderProfileUpdated = "Provider.ProfileUpdated";
    public const string ProviderServicesUpdated = "Provider.ServicesUpdated";
    public const string DocumentUploaded = "Provider.DocumentUploaded";
    public const string DocumentDeleted = "Provider.DocumentDeleted";
    public const string DocumentDownloaded = "Provider.DocumentDownloaded";

    public const string AdminDocumentViewed = "Admin.DocumentViewed";
    public const string AdminDocumentReviewed = "Admin.DocumentReviewed";
    public const string ProviderApproved = "Admin.ProviderApproved";
    public const string ProviderRejected = "Admin.ProviderRejected";
    public const string ProviderSuspended = "Admin.ProviderSuspended";
    public const string ProviderReactivated = "Admin.ProviderReactivated";

    public const string RequestCreated = "Request.Created";
    public const string RequestUpdated = "Request.Updated";
    public const string RequestCancelled = "Request.Cancelled";

    public const string OfferSubmitted = "Offer.Submitted";
    public const string OfferWithdrawn = "Offer.Withdrawn";
    public const string OfferAccepted = "Offer.Accepted";

    public const string BookingCreated = "Booking.Created";
    public const string BookingStarted = "Booking.Started";
    public const string BookingCompleted = "Booking.Completed";
    public const string BookingCancelled = "Booking.Cancelled";

    public const string ReviewCreated = "Review.Created";

    public const string CategoryCreated = "Admin.CategoryCreated";
    public const string CategoryUpdated = "Admin.CategoryUpdated";
    public const string CategoryDeleted = "Admin.CategoryDeleted";
    public const string ServiceCreated = "Admin.ServiceCreated";
    public const string ServiceUpdated = "Admin.ServiceUpdated";
    public const string ServiceDeleted = "Admin.ServiceDeleted";
}

public sealed class AuditEntry
{
    public required string Category { get; init; }

    public required string Action { get; init; }

    public required string Outcome { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? Message { get; init; }

    public IReadOnlyDictionary<string, object?>? Details { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorEmail { get; init; }

    public string? ActorRole { get; init; }
}

public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
