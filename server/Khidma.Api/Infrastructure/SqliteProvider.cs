using Microsoft.EntityFrameworkCore;

namespace Khidma.Api.Infrastructure;

internal static class SqliteProvider
{
    public const string Name = "Microsoft.EntityFrameworkCore.Sqlite";

    public static bool IsSqlite(DbContext db) =>
        string.Equals(db.Database.ProviderName, Name, StringComparison.Ordinal);
}
