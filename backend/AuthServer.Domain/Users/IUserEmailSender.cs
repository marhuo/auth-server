namespace AuthServer.Domain.Users;

public interface IUserEmailSender
{
    // An invitation. appName: the app the user is invited to (Client.DisplayName), or null for the auth
    // server itself. tenantName: the organization the user joins, or null. appLoginUri: the app's sign-in
    // address (Client.InitiateLoginUri), or null; then the email links to the auth server's sign-in page.
    Task SendInviteAsync(
        string toEmail, string? appName, string? tenantName, string? appLoginUri, EmailLanguage language);

    // A six-digit code for signing in (SignInCode), and a link that fills it in. appName: as above.
    // returnUrl is the local address where the user continues after signing in (e.g. the app's sign-in request), or null.
    Task SendSignInCodeAsync(string toEmail, string code, string? appName, string? returnUrl, EmailLanguage language);
}
