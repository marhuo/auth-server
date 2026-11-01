using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Application.Users;
using AuthServer.Web.Dto.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthServerApiScope")]
public class TenantController(TenantService tenantService, UserService userService, ClientService clientService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TenantCreateResponse>> Create(TenantCreateRequest request)
    {
        var clientId = User.FindFirstValue("client_id");
        if (clientId is null) return Unauthorized();

        var client = await clientService.GetByClientIdAsync(clientId);
        if (client is null) return Unauthorized();


        var tenantId = await tenantService.CreateAsync(request.Name);
        var userId = await userService.InviteAsync(request.AdminEmail, tenantId, client.ResourceServerId, request.AdminRoleNames);

        return new TenantCreateResponse { TenantId = tenantId, UserId = userId };
    }
}