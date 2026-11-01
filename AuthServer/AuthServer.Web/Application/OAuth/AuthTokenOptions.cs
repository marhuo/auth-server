//AuthTokenOptions.cs
namespace AuthServer.Web.Application.OAuth;

public class AuthTokenOptions
{
    public string Issuer { get; set; } = "";
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan IdTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}