using AuthServer.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthServer.Infrastructure.Users;

// Development implementation: writes the links to the log instead of sending an email.
public class LoggingUserEmailSender(ILogger<LoggingUserEmailSender> logger, IConfiguration configuration) : IUserEmailSender
{
    public Task SendInviteAsync(string toEmail, string token)
    {
        logger.LogInformation("Invite for {Email}: {BaseUrl}/SetPassword?token={Token}",
            toEmail, configuration["Tokens:Issuer"], token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string token)
    {
        logger.LogInformation("Password reset for {Email}: {BaseUrl}/SetPassword?token={Token}",
            toEmail, configuration["Tokens:Issuer"], token);
        return Task.CompletedTask;
    }
}
