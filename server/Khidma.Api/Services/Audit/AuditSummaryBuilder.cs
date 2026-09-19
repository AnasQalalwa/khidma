using System.Globalization;
using System.Reflection;
using Khidma.Api.Contracts.Audit;
using Khidma.Api.Data;
using Khidma.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Services.Audit;

public static class AuditSummaryBuilder
{
    public static AuditFilterOptionsDto FilterOptions()
    {
        var actions = typeof(AuditActions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Select(value => new AuditActionOptionDto
            {
                Value = value,
                Label = HumanizeAction(value),
                Category = CategoryOf(value)
            })
            .ToList();

        var categories = typeof(AuditCategories)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        return new AuditFilterOptionsDto
        {
            Categories = categories,
            Actions = actions
        };
    }

    public static async Task<IReadOnlyDictionary<long, string>> BuildAsync(
        AppDbContext db,
        IReadOnlyList<AuditLog> logs,
        CancellationToken cancellationToken)
    {
        var names = await ResolveNamesAsync(db, logs, cancellationToken);
        return logs.ToDictionary(
            log => log.Id,
            log => Render(log, names));
    }

    public static async Task<string> BuildAsync(
        AppDbContext db,
        AuditLog log,
        CancellationToken cancellationToken)
    {
        var map = await BuildAsync(db, [log], cancellationToken);
        return map[log.Id];
    }

    private static async Task<Dictionary<(string Type, string Id), string>> ResolveNamesAsync(
        AppDbContext db,
        IReadOnlyList<AuditLog> logs,
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<(string Type, string Id), string>();
        var groups = logs
            .Where(log => !string.IsNullOrWhiteSpace(log.EntityType) &&
                          !string.IsNullOrWhiteSpace(log.EntityId))
            .GroupBy(log => log.EntityType!, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            switch (group.Key)
            {
                case nameof(ProviderProfile):
                    await AddIntNames(
                        names,
                        nameof(ProviderProfile),
                        ParseInts(group),
                        ids => db.ProviderProfiles
                            .AsNoTracking()
                            .Where(p => ids.Contains(p.Id))
                            .Select(p => new IdName(p.Id.ToString(), p.User.FullName)),
                        cancellationToken);
                    break;
                case nameof(ProviderVerificationDocument):
                    await AddIntNames(
                        names,
                        nameof(ProviderVerificationDocument),
                        ParseInts(group),
                        ids => db.ProviderVerificationDocuments
                            .AsNoTracking()
                            .Where(d => ids.Contains(d.Id))
                            .Select(d => new IdName(
                                d.Id.ToString(),
                                d.ProviderProfile.User.FullName)),
                        cancellationToken);
                    break;
                case nameof(ServiceRequest):
                    await AddIntNames(
                        names,
                        nameof(ServiceRequest),
                        ParseInts(group),
                        ids => db.ServiceRequests
                            .AsNoTracking()
                            .Where(r => ids.Contains(r.Id))
                            .Select(r => new IdName(r.Id.ToString(), r.Title)),
                        cancellationToken);
                    break;
                case nameof(Offer):
                    await AddIntNames(
                        names,
                        nameof(Offer),
                        ParseInts(group),
                        ids => db.Offers
                            .AsNoTracking()
                            .Where(o => ids.Contains(o.Id))
                            .Select(o => new IdName(o.Id.ToString(), o.ServiceRequest.Title)),
                        cancellationToken);
                    break;
                case nameof(Booking):
                    await AddIntNames(
                        names,
                        nameof(Booking),
                        ParseInts(group),
                        ids => db.Bookings
                            .AsNoTracking()
                            .Where(b => ids.Contains(b.Id))
                            .Select(b => new IdName(b.Id.ToString(), b.ServiceRequest.Title)),
                        cancellationToken);
                    break;
                case nameof(Review):
                    await AddIntNames(
                        names,
                        nameof(Review),
                        ParseInts(group),
                        ids => db.Reviews
                            .AsNoTracking()
                            .Where(r => ids.Contains(r.Id))
                            .Select(r => new IdName(r.Id.ToString(), r.Provider.FullName)),
                        cancellationToken);
                    break;
                case nameof(Category):
                    await AddIntNames(
                        names,
                        nameof(Category),
                        ParseInts(group),
                        ids => db.Categories
                            .AsNoTracking()
                            .Where(c => ids.Contains(c.Id))
                            .Select(c => new IdName(c.Id.ToString(), c.Name)),
                        cancellationToken);
                    break;
                case nameof(Service):
                    await AddIntNames(
                        names,
                        nameof(Domain.Service),
                        ParseInts(group),
                        ids => db.Services
                            .AsNoTracking()
                            .Where(s => ids.Contains(s.Id))
                            .Select(s => new IdName(s.Id.ToString(), s.Name)),
                        cancellationToken);
                    break;
                case nameof(ApplicationUser):
                    var userIds = group.Select(log => log.EntityId!).Distinct().ToList();
                    var users = await db.Users
                        .AsNoTracking()
                        .Where(user => userIds.Contains(user.Id))
                        .Select(user => new IdName(user.Id, user.FullName))
                        .ToListAsync(cancellationToken);
                    foreach (var user in users)
                    {
                        if (!LooksLikeRawId(user.Name))
                        {
                            names[(nameof(ApplicationUser), user.Id)] = user.Name;
                        }
                    }

                    break;
            }
        }

        return names;
    }

    private static async Task AddIntNames(
        Dictionary<(string Type, string Id), string> names,
        string entityType,
        List<int> ids,
        Func<List<int>, IQueryable<IdName>> query,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var rows = await query(ids).ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            if (!LooksLikeRawId(row.Name))
            {
                names[(entityType, row.Id)] = row.Name;
            }
        }
    }

    private static List<int> ParseInts(IEnumerable<AuditLog> logs) =>
        logs
            .Select(log => log.EntityId)
            .Where(id => int.TryParse(id, out _))
            .Select(id => int.Parse(id!, CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();

    private static string Render(
        AuditLog log,
        IReadOnlyDictionary<(string Type, string Id), string> names)
    {
        var actor = string.IsNullOrWhiteSpace(log.ActorRole) ? "Someone" : log.ActorRole;
        var name = DisplayName(log, names);
        var quoted = string.IsNullOrWhiteSpace(name) ? null : $"'{name}'";

        var summary = log.Action switch
        {
            AuditActions.RegisterSucceeded =>
                string.IsNullOrWhiteSpace(name) ? $"{actor} registered." : $"{name} registered.",
            AuditActions.LoginSucceeded =>
                string.IsNullOrWhiteSpace(name) ? $"{actor} signed in." : $"{name} signed in.",
            AuditActions.LoginFailed => "Sign-in failed.",
            AuditActions.Logout =>
                string.IsNullOrWhiteSpace(name) ? $"{actor} signed out." : $"{name} signed out.",
            AuditActions.CsrfRejected => "Security token was rejected.",
            AuditActions.ProviderProfileUpdated =>
                string.IsNullOrWhiteSpace(name)
                    ? $"{actor} updated a provider profile."
                    : $"{name} updated their provider profile.",
            AuditActions.ProviderServicesUpdated =>
                string.IsNullOrWhiteSpace(name)
                    ? $"{actor} updated provider services."
                    : $"{name} updated their services.",
            AuditActions.DocumentUploaded =>
                string.IsNullOrWhiteSpace(name)
                    ? $"{actor} uploaded a verification document."
                    : $"{name} uploaded a verification document.",
            AuditActions.DocumentDeleted =>
                string.IsNullOrWhiteSpace(name)
                    ? $"{actor} deleted a verification document."
                    : $"{name} deleted a verification document.",
            AuditActions.DocumentDownloaded =>
                string.IsNullOrWhiteSpace(name)
                    ? $"{actor} downloaded a verification document."
                    : $"{name} downloaded a verification document.",
            AuditActions.AdminDocumentViewed =>
                string.IsNullOrWhiteSpace(name)
                    ? "Admin viewed a verification document."
                    : $"Admin viewed a document for {name}.",
            AuditActions.AdminDocumentReviewed =>
                string.IsNullOrWhiteSpace(name)
                    ? "Admin reviewed a verification document."
                    : $"Admin reviewed a document for {name}.",
            AuditActions.ProviderApproved =>
                string.IsNullOrWhiteSpace(name)
                    ? "Admin approved a provider."
                    : $"Admin approved provider {name}",
            AuditActions.ProviderRejected =>
                string.IsNullOrWhiteSpace(name)
                    ? "Admin rejected a provider."
                    : $"Admin rejected provider {name}",
            AuditActions.ProviderSuspended =>
                string.IsNullOrWhiteSpace(name)
                    ? "Admin suspended a provider."
                    : $"Admin suspended provider {name}",
            AuditActions.ProviderReactivated =>
                string.IsNullOrWhiteSpace(name)
                    ? "Admin reactivated a provider."
                    : $"Admin reactivated provider {name}",
            AuditActions.RequestCreated =>
                quoted is null
                    ? $"{actor} created a request."
                    : $"{actor} created request {quoted}",
            AuditActions.RequestUpdated =>
                quoted is null
                    ? $"{actor} updated a request."
                    : $"{actor} updated request {quoted}",
            AuditActions.RequestCancelled =>
                quoted is null
                    ? $"{actor} cancelled a request."
                    : $"{actor} cancelled request {quoted}",
            AuditActions.OfferSubmitted =>
                quoted is null
                    ? $"{actor} submitted an offer."
                    : $"{actor} submitted an offer on {quoted}",
            AuditActions.OfferWithdrawn =>
                quoted is null
                    ? $"{actor} withdrew an offer."
                    : $"{actor} withdrew an offer on {quoted}",
            AuditActions.OfferAccepted =>
                quoted is null
                    ? $"{actor} accepted an offer."
                    : $"{actor} accepted an offer on {quoted}",
            AuditActions.BookingCreated =>
                quoted is null
                    ? $"{actor} created a booking."
                    : $"{actor} created a booking for {quoted}",
            AuditActions.BookingStarted =>
                quoted is null
                    ? $"{actor} started a booking."
                    : $"{actor} started a booking for {quoted}",
            AuditActions.BookingCompleted =>
                quoted is null
                    ? $"{actor} completed a booking."
                    : $"{actor} completed a booking for {quoted}",
            AuditActions.BookingCancelled =>
                quoted is null
                    ? $"{actor} cancelled a booking."
                    : $"{actor} cancelled a booking for {quoted}",
            AuditActions.ReviewCreated =>
                string.IsNullOrWhiteSpace(name)
                    ? $"{actor} left a review."
                    : $"{actor} left a review for {name}.",
            AuditActions.CategoryCreated =>
                quoted is null ? "Admin created a category." : $"Admin created category {quoted}",
            AuditActions.CategoryUpdated =>
                quoted is null ? "Admin updated a category." : $"Admin updated category {quoted}",
            AuditActions.CategoryDeleted =>
                quoted is null ? "Admin deleted a category." : $"Admin deleted category {quoted}",
            AuditActions.ServiceCreated =>
                quoted is null ? "Admin created a service." : $"Admin created service {quoted}",
            AuditActions.ServiceUpdated =>
                quoted is null ? "Admin updated a service." : $"Admin updated service {quoted}",
            AuditActions.ServiceDeleted =>
                quoted is null ? "Admin deleted a service." : $"Admin deleted service {quoted}",
            _ => Fallback(log, actor, name)
        };

        if (log.Outcome == AuditOutcomes.Denied)
        {
            return summary.StartsWith("Denied", StringComparison.Ordinal)
                ? summary
                : $"Denied: {TrimPeriod(summary)}";
        }

        if (log.Outcome == AuditOutcomes.Failed)
        {
            return summary.StartsWith("Sign-in failed", StringComparison.Ordinal) ||
                   summary.StartsWith("Failed", StringComparison.Ordinal)
                ? summary
                : $"Failed: {TrimPeriod(summary)}";
        }

        return summary;
    }

    private static string Fallback(AuditLog log, string actor, string? name)
    {
        var verb = HumanizeAction(log.Action).ToLowerInvariant();
        var entity = string.IsNullOrWhiteSpace(log.EntityType)
            ? "an item"
            : HumanizeToken(log.EntityType);
        if (string.IsNullOrWhiteSpace(name))
        {
            return $"{actor} {verb} {entity}.";
        }

        return $"{actor} {verb} {entity} {name}.";
    }

    private static string? DisplayName(
        AuditLog log,
        IReadOnlyDictionary<(string Type, string Id), string> names)
    {
        if (string.IsNullOrWhiteSpace(log.EntityType) || string.IsNullOrWhiteSpace(log.EntityId))
        {
            return null;
        }

        return names.TryGetValue((log.EntityType, log.EntityId), out var name) ? name : null;
    }

    public static string HumanizeAction(string action)
    {
        var leaf = action;
        var dot = action.LastIndexOf('.');
        if (dot >= 0 && dot < action.Length - 1)
        {
            leaf = action[(dot + 1)..];
        }

        return HumanizeToken(leaf);
    }

    private static string HumanizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        var chars = new List<char> { token[0] };
        for (var i = 1; i < token.Length; i++)
        {
            if (char.IsUpper(token[i]) && !char.IsUpper(token[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(char.ToLowerInvariant(token[i]));
        }

        chars[0] = char.ToUpperInvariant(chars[0]);
        return new string(chars.ToArray());
    }

    private static string CategoryOf(string action)
    {
        var dot = action.IndexOf('.');
        return dot > 0 ? action[..dot] : action;
    }

    private static bool LooksLikeRawId(string value) =>
        Guid.TryParse(value, out _) ||
        (value.Length >= 20 && value.All(ch => char.IsDigit(ch) || ch is '-' or '{'));

    private static string TrimPeriod(string value) =>
        value.EndsWith('.') ? value[..^1] : value;

    private sealed record IdName(string Id, string Name);
}
