using AuthServer.Web.Authentication;
using AuthServer.Application.Roles.Services;
using AuthServer.Application.Users.Services;
using AuthServer.Web.Controllers.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppUserInviteRequest = AuthServer.Application.Users.Messages.UserInviteRequest;
using AppUserRoleDto = AuthServer.Application.Users.Messages.UserRoleDto;

namespace AuthServer.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ExternalApi)]
public class UserController(UserService userService, RoleService roleService) : ControllerBase
{
    [HttpPost("invite")]
    public async Task<ActionResult<UserInviteResponse>> Invite(UserInviteRequest request)
    {
        var roleIds = await roleService.GetRequiredProductRoleIdsAsync(request.RoleNames, nameof(request.RoleNames));

        var userId = await userService.InviteAsync(new AppUserInviteRequest
        {
            Email = request.Email,
            ClientId = request.ClientId,
            // Always with the tenant: the external API must never give auth server roles, and an
            // auth server role with a tenant is rejected by the validator.
            UserRoles = roleIds.Select(id => new AppUserRoleDto { RoleId = id, TenantId = request.TenantId }).ToList()
        });

        return new UserInviteResponse { UserId = userId };
    }
}
