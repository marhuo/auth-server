using AuthServer.Web.Authentication;
using AuthServer.Application.Clients.Services;
using AuthServer.Application.Roles.Services;
using AuthServer.Application.Tenants.Services;
using AuthServer.Application.Users.Services;
using AuthServer.Web.Controllers.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppTenantCreateRequest = AuthServer.Application.Tenants.Messages.TenantCreateRequest;
using AppUserInviteRequest = AuthServer.Application.Users.Messages.UserInviteRequest;
using AppUserRoleDto = AuthServer.Application.Users.Messages.UserRoleDto;

namespace AuthServer.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ExternalApi)]
public class TenantController(
    TenantService tenantService, UserService userService, RoleService roleService, ClientService clientService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TenantCreateResponse>> Create(TenantCreateRequest request)
    {
        var clientId = User.FindFirstValue("client_id");
        if (clientId is null) return Unauthorized();

        var client = await clientService.GetByClientIdAsync(clientId);
        if (client is null) return Unauthorized();

        // Resolved before the tenant is created, so unknown role names do not leave a tenant without an admin.
        var roleIds = await roleService.GetRequiredIdsByNamesAsync(
            client.ResourceServerId, request.AdminRoleNames, nameof(request.AdminRoleNames));

        var tenantId = await tenantService.CreateAsync(new AppTenantCreateRequest { Name = request.Name });
        var userId = await userService.InviteAsync(new AppUserInviteRequest
        {
            Email = request.AdminEmail,
            // Always with the tenant: the external API must never give auth server roles, and an
            // auth server role with a tenant is rejected by the validator.
            UserRoles = roleIds.Select(id => new AppUserRoleDto { RoleId = id, TenantId = tenantId }).ToList()
        });

        // The slug is generated from the name when the tenant is created.
        var tenant = await tenantService.GetByIdAsync(tenantId)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found after it was created.");

        return new TenantCreateResponse { TenantId = tenantId, UserId = userId, Slug = tenant.Slug };
    }
}
