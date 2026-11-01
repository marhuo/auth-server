using AuthServer.Domain.Clients;
using AuthServer.Application.OAuth.Services;
using AuthServer.Application.Tenants.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Web.Controllers.OAuth;

[ApiController]
[Route("connect/userinfo")]
[Authorize(AuthenticationSchemes = "UserInfo")]
public class UserInfoController(UserAuthenticationService userService, TenantService tenantService, ClientAuthenticationService clientService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var scopes = User.FindFirstValue("scope")?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (!scopes.Contains("openid") || !Guid.TryParse(User.FindFirstValue("sub"), out var userId))
            return Forbid();

        var user = await userService.GetByIdAsync(userId);
        if (user is null) return Unauthorized();

        var result = new Dictionary<string, object> { ["sub"] = user.Id.ToString() };
        // Same as the ID token (TokenService): the email is the name.
        if (scopes.Contains("profile"))
        {
            result["name"] = user.Email;
            result["preferred_username"] = user.Email;
        }
        if (scopes.Contains("email")) result["email"] = user.Email;

        var client = await clientService.FindActiveAsync(User.FindFirstValue("client_id") ?? "");
        if (client is not null)
        {
            var tenants = await tenantService.GetUserTenantsAsync(userId, client.TargetResourceServerId);
            result["tenants"] = tenants.Select(t => new { id = t.Id, name = t.Name });
        }

        return Ok(result);
    }
}
