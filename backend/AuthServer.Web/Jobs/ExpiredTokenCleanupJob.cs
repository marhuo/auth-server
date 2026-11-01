using AuthServer.Application.Common.Services;
using Hangfire;

namespace AuthServer.Web.Jobs;

// Deletes expired authorization codes, refresh tokens and password tokens. Scheduled in HangfireSetup.
public class ExpiredTokenCleanupJob(ExpiredTokenCleanupService cleanupService, ILogger<ExpiredTokenCleanupJob> logger)
{
    public const string JobId = "expired-token-cleanup";

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync()
    {
        var deletedCount = await cleanupService.DeleteExpiredTokensAsync();
        logger.LogInformation("Deleted {Count} expired tokens", deletedCount);
    }
}
