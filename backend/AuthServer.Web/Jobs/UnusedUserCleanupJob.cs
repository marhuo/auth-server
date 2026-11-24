using AuthServer.Application.Users.Services;
using Hangfire;

namespace AuthServer.Web.Jobs;

// Deletes users who never started using their account. Scheduled in HangfireSetup.
public class UnusedUserCleanupJob(UserService userService, ILogger<UnusedUserCleanupJob> logger)
{
    public const string JobId = "unused-user-cleanup";

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync()
    {
        var deletedCount = await userService.DeleteUnusedInvitedAsync();
        logger.LogInformation("Deleted {Count} unused invited users", deletedCount);
    }
}
