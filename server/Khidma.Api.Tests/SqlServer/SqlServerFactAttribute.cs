namespace Khidma.Api.Tests.SqlServer;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerTestSettings.IsEnabled)
        {
            Skip = "Set KHIDMA_SQLSERVER_TESTS=1 to run against SQL Server.";
        }
    }
}
