using AuthServer.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthServer.Infrastructure.Users;

// Development implementation: writes the links to the log instead of sending an email.
public class LoggingUserEmailSender(ILogger<LoggingUserEmailSender> logger, IConfiguration configuration) : IUserEmailSender
{
    // ":l" writes the string as is: by default Serilog puts quotes around string values in the message,
    // and the link could not be copied from the log.
    public Task SendInviteAsync(string toEmail, string token, string? clientId)
    {
        logger.LogInformation("Invite for {Email:l}: {Link:l}", toEmail,
            SetPasswordLink(token, returnUrl: null) + (clientId is null ? "" : $"&clientId={Uri.EscapeDataString(clientId)}"));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string token)
    {
        logger.LogInformation("Password reset for {Email:l}: {Link:l}", toEmail, SetPasswordLink(token, returnUrl: null));
        return Task.CompletedTask;
    }

    public Task SendRegistrationAsync(string toEmail, string token, string? returnUrl)
    {
        logger.LogInformation("Registration for {Email:l}: {Link:l}", toEmail, SetPasswordLink(token, returnUrl));
        return Task.CompletedTask;
    }

    public Task SendAlreadyRegisteredAsync(string toEmail, string? returnUrl)
    {
        logger.LogInformation("Already registered, sign in instead, for {Email:l}: {Link:l}", toEmail,
            Link("/Login", returnUrl is null ? "" : $"?returnUrl={Uri.EscapeDataString(returnUrl)}"));
        return Task.CompletedTask;
    }

    private string SetPasswordLink(string token, string? returnUrl) =>
        Link("/SetPassword", $"?token={Uri.EscapeDataString(token)}"
            + (returnUrl is null ? "" : $"&returnUrl={Uri.EscapeDataString(returnUrl)}"));

    private string Link(string path, string query) => $"{configuration["Tokens:Issuer"]}{path}{query}";
}
