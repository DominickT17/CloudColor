using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudColor.Web.Services;

public sealed class SqlReadyHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("CloudColor");
            if (string.IsNullOrWhiteSpace(connectionString)) return HealthCheckResult.Unhealthy();
            var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 3, ConnectRetryCount = 0 };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = configuration.GetValue<bool>("CloudColor:UseSqlDataProtection")
                ? "SELECT CASE WHEN OBJECT_ID(N'dbo.AspNetDataProtectionKeys', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'CloudColorDockerSchemaVersion' AND CONVERT(nvarchar(10),value)=N'2') THEN 1 ELSE 0 END"
                : "SELECT 1";
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1
                ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
