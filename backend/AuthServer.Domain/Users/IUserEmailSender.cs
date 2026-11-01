namespace AuthServer.Domain.Users;

public interface IUserEmailSender
{
    // clientId: the client the invited user signs in with (UserInviteRequest.ClientId), or null.
    Task SendInviteAsync(string toEmail, string token, string? clientId);

    Task SendPasswordResetAsync(string toEmail, string token);

    // Self-registration: a link to set the first password. returnUrl is the local address where the
    // user continues after setting it (e.g. the product's sign-in request), or null.
    Task SendRegistrationAsync(string toEmail, string token, string? returnUrl);

    // Someone registered with an email that already has an account: tells the owner to sign in.
    Task SendAlreadyRegisteredAsync(string toEmail, string? returnUrl);
}
