namespace AuthServer.Domain.Users;

public interface IUserEmailSender
{
    // An invitation. appName and appLoginUri: the app the user is invited to (the client's display name
    // and initiate login URI), or null; then the email links to the auth server's sign-in page.
    Task SendInviteAsync(string toEmail, string? appName, string? appLoginUri);

    // A six-digit code for signing in (SignInCode), and a link that fills it in. returnUrl is the local
    // address where the user continues after signing in (e.g. the product's sign-in request), or null.
    Task SendSignInCodeAsync(string toEmail, string code, string? returnUrl);
}
