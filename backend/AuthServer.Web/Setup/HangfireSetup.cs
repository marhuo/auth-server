using AuthServer.Web.Jobs;
using Hangfire;
using Hangfire.PostgreSql;

namespace AuthServer.Web.Setup;

public static class HangfireSetup
{
    private const string SchemaName = "hangfire";

    // How often the queue and the recurring job schedule are polled (default 15 s each). The jobs run
    // nightly, so a delay of up to two minutes does not matter; "Trigger now" in the dashboard may
    // also take up to a minute to start.
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, string connectionString)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = SchemaName,
                    QueuePollInterval = PollInterval
                }));

        services.AddHangfireServer(options =>
        {
            // Only a few nightly jobs: the default (5 x CPU cores) would mostly poll the queue for nothing.
            options.WorkerCount = 2;
            options.SchedulePollingInterval = PollInterval;
        });

        return services;
    }

    public const string DashboardPath = "/hangfire";

    // Maps the dashboard behind the given authorization policy. Unauthenticated users are redirected
    // to the login page by the cookie scheme. Call after UseAuthorization.
    public static WebApplication UseBackgroundJobs(this WebApplication app, string authorizationPolicy)
    {
        app.MapHangfireDashboardWithAuthorizationPolicy(authorizationPolicy, DashboardPath);

        var jobs = app.Services.GetRequiredService<IRecurringJobManager>();

        // Cron times are UTC.
        jobs.AddOrUpdate<SerilogCleanupJob>(
            SerilogCleanupJob.JobId,
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(3));

        jobs.AddOrUpdate<ExpiredTokenCleanupJob>(
            ExpiredTokenCleanupJob.JobId,
            job => job.RunAsync(),
            Cron.Daily(3));

        jobs.AddOrUpdate<TenantDomainRecheckJob>(
            TenantDomainRecheckJob.JobId,
            job => job.RunAsync(),
            Cron.Weekly(DayOfWeek.Monday, 4));

        return app;
    }
}
