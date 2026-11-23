using AuthServer.Application.Tenants.Services;
using Hangfire;

namespace AuthServer.Web.Jobs;

// Removes the verification of domains whose TXT record has disappeared. Scheduled in HangfireSetup.
public class TenantDomainRecheckJob(TenantDomainService domainService, ILogger<TenantDomainRecheckJob> logger)
{
    public const string JobId = "tenant-domain-recheck";

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync()
    {
        var unverifiedCount = await domainService.RecheckVerifiedAsync();
        logger.LogInformation("{Count} domains lost their verification", unverifiedCount);
    }
}
