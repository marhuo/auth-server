using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using Serilog.Ui.Core.Extensions;
using Serilog.Ui.PostgreSqlProvider.Extensions;
using Serilog.Ui.Web.Extensions;

namespace AuthServer.Web.Setup;


public static class SerilogSetup
{
    public const string TableName = "logs";

    public static IServiceCollection AddLogging(this IServiceCollection services, string connectionString)
    {
        EnsureLogTable(connectionString);

        var columns = new Dictionary<string, ColumnWriterBase>
        {
            ["Message"] = new RenderedMessageColumnWriter(),
            ["MessageTemplate"] = new MessageTemplateColumnWriter(),
            ["Level"] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
            ["Timestamp"] = new UtcTimestampColumnWriter(),
            ["Exception"] = new ExceptionColumnWriter(),
            ["LogEvent"] = new LogEventSerializedColumnWriter()
        };

        services.AddSerilog(config => config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.PostgreSQL(connectionString, TableName, columns, needAutoCreateTable: false));

        services.AddSerilogUi(options => options
            .UseNpgSql(opt => opt
                .WithConnectionString(connectionString)
                .WithTable(TableName)));

        return services;
    }

    // The table is created here instead of by the sink (needAutoCreateTable: false) so that its
    // indexes can be created in the same place. The column names must match the column writers above.
    private static void EnsureLogTable(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                "Message" text,
                "MessageTemplate" text,
                "Level" varchar(50),
                "Timestamp" timestamptz,
                "Exception" text,
                "LogEvent" jsonb
            );
            CREATE INDEX IF NOT EXISTS ix_{TableName}_timestamp ON {TableName} ("Timestamp");
            """;
        command.ExecuteNonQuery();
    }

    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }

    // Serilog UI is middleware, not an endpoint, so RequireAuthorization cannot be used; the policy
    // is checked here instead. Call after UseAuthorization.
    public static WebApplication UseLogViewer(this WebApplication app, string authorizationPolicy)
    {
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/serilog-ui"),
            branch => branch.Use(async (context, next) =>
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    await context.ChallengeAsync();
                    return;
                }

                var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
                if (!(await authorization.AuthorizeAsync(context.User, authorizationPolicy)).Succeeded)
                {
                    await context.ForbidAsync();
                    return;
                }

                await next();
            }));

        app.UseSerilogUi();
        return app;
    }
}
public class UtcTimestampColumnWriter() : ColumnWriterBase(NpgsqlDbType.TimestampTz)
{
    public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null) =>
        logEvent.Timestamp.UtcDateTime;
}