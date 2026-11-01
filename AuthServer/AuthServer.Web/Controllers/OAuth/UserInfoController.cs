using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Web.Controllers.OAuth;

[ApiController]
[Route("connect/userinfo")]
[Authorize(AuthenticationSchemes = "UserInfo")]
public class UserInfoController(UserService userService, TenantService tenantService, ClientService clientService, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var scopes = User.FindFirstValue("scope")?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (!scopes.Contains("openid") || !Guid.TryParse(User.FindFirstValue("sub"), out var userId))
            return Forbid();

        var user = await userService.GetActiveIdentityAsync(userId);
        if (user is null) return Unauthorized();

        var result = new Dictionary<string, object> { ["sub"] = user.Id.ToString() };
        if (scopes.Contains("profile")) result["name"] = user.UserName;
        if (scopes.Contains("email")) result["email"] = user.Email;

        var client = await clientService.FindActiveAsync(User.FindFirstValue("client_id") ?? "");
        if (client is not null)
        {
            var tenants = await tenantService.GetUserTenantsAsync(userId, client.ResourceServerId, timeProvider.GetUtcNow());
            result["tenants"] = tenants.Select(t => new { id = t.Id, name = t.Name });
        }

        return Ok(result);
    }
}
