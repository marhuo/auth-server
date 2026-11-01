namespace AuthServer.Domain.Users;

public interface IUserEmailSender
{
    Task SendInviteAsync(string toEmail, string token);

    Task SendPasswordResetAsync(string toEmail, string token);
}
