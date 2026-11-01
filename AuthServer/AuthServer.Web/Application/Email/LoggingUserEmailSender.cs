namespace AuthServer.Web.Application.Email;

public class LoggingUserEmailSender(ILogger<LoggingUserEmailSender> logger, IConfiguration configuration) : IUserEmailSender
{
    public Task SendInviteAsync(string toEmail, string token)
    {
        var baseUrl = configuration["Tokens:Issuer"];
        logger.LogInformation("Invite for {Email}: {BaseUrl}/SetPassword?token={Token}",
            toEmail, baseUrl, token);
        return Task.CompletedTask;
    }
}