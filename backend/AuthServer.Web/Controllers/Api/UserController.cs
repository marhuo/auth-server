using AuthServer.Web.Authentication;
using AuthServer.Application.Clients.Services;
using AuthServer.Application.Roles.Services;
using AuthServer.Application.Users.Services;
using AuthServer.Web.Controllers.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppUserInviteRequest = AuthServer.Application.Users.Messages.UserInviteRequest;
using AppUserRoleDto = AuthServer.Application.Users.Messages.UserRoleDto;

namespace AuthServer.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ExternalApi)]
public class UserController(UserService userService, RoleService roleService, ClientService clientService) : ControllerBase
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

        var roleIds = await roleService.GetRequiredIdsByNamesAsync(
            client.ResourceServerId, request.RoleNames, nameof(request.RoleNames));

        var userId = await userService.InviteAsync(new AppUserInviteRequest
        {
            Email = request.Email,
            // Always with the tenant: the external API must never give auth server roles, and an
            // auth server role with a tenant is rejected by the validator.
            UserRoles = roleIds.Select(id => new AppUserRoleDto { RoleId = id, TenantId = request.TenantId }).ToList()
        });

        return new UserInviteResponse { UserId = userId };
    }
}
