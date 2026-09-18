namespace Khidma.Api.Tests.SqlServer;

internal static class SqlServerTestSettings
{
    public const string EnabledEnvironmentVariable = "KHIDMA_SQLSERVER_TESTS";

    public const string ConnectionEnvironmentVariable = "KHIDMA_SQLSERVER_CONNECTION";

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnabledEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)
        ?? "Server=(localdb)\\mssqllocaldb;Database=Khidma_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
}
