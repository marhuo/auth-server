using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Users;
using AuthServer.Web.Dto.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthServerApiScope")]
public class UserController(UserService userService, ClientService clientService) : ControllerBase
{
    [HttpPost("invite")]
    public async Task<ActionResult<UserInviteResponse>> Invite(UserInviteRequest request)
    {
        var clientId = User.FindFirstValue("client_id");
        if (clientId is null)
            return Unauthorized();

        var client = await clientService.GetByClientIdAsync(clientId);
        if (client is null)
            return Unauthorized();

        var userId = await userService.InviteAsync(request.Email, request.TenantId, client.ResourceServerId, request.RoleNames);

        return new UserInviteResponse { UserId = userId };
    }
}