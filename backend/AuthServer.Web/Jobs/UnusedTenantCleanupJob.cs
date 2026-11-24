using AuthServer.Application.Tenants.Services;
using Hangfire;

namespace AuthServer.Web.Jobs;

// Deletes tenants that nothing points to. Scheduled in HangfireSetup.
public class UnusedTenantCleanupJob(TenantService tenantService, ILogger<UnusedTenantCleanupJob> logger)
{
    public const string JobId = "unused-tenant-cleanup";

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync()
    {
        var deletedCount = await tenantService.DeleteUnusedAsync();
        logger.LogInformation("Deleted {Count} unused tenants", deletedCount);
    }
}
