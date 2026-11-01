using AuthServer.Web.Setup;
using Hangfire;
using Npgsql;

namespace AuthServer.Web.Jobs;

// Deletes log rows older than Logs:RetentionDays (default 30). Scheduled in HangfireSetup.
public class SerilogCleanupJob(IConfiguration configuration, TimeProvider timeProvider, ILogger<SerilogCleanupJob> logger)
{
    public const string JobId = "serilog-cleanup";

    private const int DefaultRetentionDays = 30;

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var retentionDays = configuration.GetValue("Logs:RetentionDays", DefaultRetentionDays);
        var cutoff = timeProvider.GetUtcNow().AddDays(-retentionDays);

        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Logs"));
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""DELETE FROM {SerilogSetup.TableName} WHERE "Timestamp" < @cutoff""";
        command.Parameters.AddWithValue("cutoff", cutoff);

        var deletedCount = await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Deleted {Count} log rows older than {Cutoff}", deletedCount, cutoff);
    }
}
