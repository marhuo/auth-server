using AuthServer.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthServer.Infrastructure.Users;

// Development implementation: writes the emails to the log instead of sending them. The HTML can be
// copied from the log into a file and opened in a browser.
public class LoggingUserEmailSender(ILogger<LoggingUserEmailSender> logger, IConfiguration configuration) : IUserEmailSender
{
    public Task SendInviteAsync(
        string toEmail, string? appName, string? tenantName, string? appLoginUri, EmailLanguage language)
    {
        // login_hint (OpenID Connect): the app passes it on, and the sign-in page has the email filled in.
        var link = appLoginUri is null
            ? $"{Issuer}/Login?email={Uri.EscapeDataString(toEmail)}"
            : $"{appLoginUri}{(appLoginUri.Contains('?') ? '&' : '?')}login_hint={Uri.EscapeDataString(toEmail)}";
        Log("Invite", toEmail, EmailTemplates.InviteEmail(toEmail, link, AppName(appName), tenantName ?? AppName(appName), language));
        return Task.CompletedTask;
    }

    public Task SendSignInCodeAsync(string toEmail, string code, string? appName, string? returnUrl, EmailLanguage language)
    {
        var link = $"{Issuer}/Login?email={Uri.EscapeDataString(toEmail)}&code={code}"
            + (returnUrl is null ? "" : $"&returnUrl={Uri.EscapeDataString(returnUrl)}");

        logger.LogInformation("Sign-in code for {Email:l}: {Code:l}", toEmail, code);
        Log("Sign-in code", toEmail, EmailTemplates.SignInCodeEmail(code, link, AppName(appName), language));
        return Task.CompletedTask;
    }

    private string? Issuer => configuration["Tokens:Issuer"];

    // Without an app the email is about the auth server itself: its own name (Email:AppName).
    private string AppName(string? appName) => appName ?? configuration["Email:AppName"] ?? "";

    // ":l" writes the string as is: by default Serilog puts quotes around string values in the message.
    private void Log(string kind, string toEmail, EmailMessage message) =>
        logger.LogInformation("{Kind:l} email for {Email:l}\nSubject: {Subject:l}\n{Html:l}",
            kind, toEmail, message.Subject, message.Html);
}
