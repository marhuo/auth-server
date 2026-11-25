using AuthServer.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthServer.Infrastructure.Users;

// Development implementation: writes the emails to the log instead of sending them.
public class LoggingUserEmailSender(ILogger<LoggingUserEmailSender> logger, IConfiguration configuration) : IUserEmailSender
{
    // ":l" writes the string as is: by default Serilog puts quotes around string values in the message,
    // and the link could not be copied from the log.
    public Task SendInviteAsync(string toEmail, string? appName, string? appLoginUri)
    {
        var link = appLoginUri
            ?? $"{configuration["Tokens:Issuer"]}/Login?email={Uri.EscapeDataString(toEmail)}";

        logger.LogInformation("Invite for {Email:l} to {App:l}: {Link:l}", toEmail, appName ?? "the auth server", link);
        return Task.CompletedTask;
    }

    public Task SendSignInCodeAsync(string toEmail, string code, string? returnUrl)
    {
        var link = $"{configuration["Tokens:Issuer"]}/Login?email={Uri.EscapeDataString(toEmail)}&code={code}"
            + (returnUrl is null ? "" : $"&returnUrl={Uri.EscapeDataString(returnUrl)}");

        logger.LogInformation("Sign-in code for {Email:l}: {Code:l} {Link:l}", toEmail, code, link);
        return Task.CompletedTask;
    }
}
