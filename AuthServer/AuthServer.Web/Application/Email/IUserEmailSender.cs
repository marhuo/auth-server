namespace AuthServer.Web.Application.Email;

public interface IUserEmailSender
{
    Task SendInviteAsync(string toEmail, string token);
}
