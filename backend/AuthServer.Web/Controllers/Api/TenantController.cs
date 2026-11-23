using AuthServer.Web.Authentication;
using AuthServer.Application.Roles.Services;
using AuthServer.Application.Tenants.Services;
using AuthServer.Application.Users.Services;
using AuthServer.Web.Controllers.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppTenantCreateRequest = AuthServer.Application.Tenants.Messages.TenantCreateRequest;
using AppTenantDomainCreateRequest = AuthServer.Application.Tenants.Messages.TenantDomainCreateRequest;
using AppTenantDomainDto = AuthServer.Application.Tenants.Messages.TenantDomainDto;
using AppUserInviteRequest = AuthServer.Application.Users.Messages.UserInviteRequest;
using AppUserRoleDto = AuthServer.Application.Users.Messages.UserRoleDto;

namespace AuthServer.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ExternalApi)]
public class TenantController(
    TenantService tenantService,
    TenantDomainService domainService,
    UserService userService,
    RoleService roleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TenantCreateResponse>> Create(TenantCreateRequest request)
    {
        // Resolved before the tenant is created, so unknown role names do not leave a tenant without an admin.
        var roleIds = await roleService.GetRequiredProductRoleIdsAsync(
            request.AdminRoleNames, nameof(request.AdminRoleNames));

        var tenantId = await tenantService.CreateAsync(new AppTenantCreateRequest { Name = request.Name });
        var userId = await userService.InviteAsync(new AppUserInviteRequest
        {
            Email = request.AdminEmail,
            ClientId = request.ClientId,
            // Always with the tenant: the external API must never give auth server roles, and an
            // auth server role with a tenant is rejected by the validator.
            UserRoles = roleIds.Select(id => new AppUserRoleDto { RoleId = id, TenantId = tenantId }).ToList()
        });

        // The slug is generated from the name when the tenant is created.
        var tenant = await tenantService.GetByIdAsync(tenantId)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found after it was created.");

        return new TenantCreateResponse { TenantId = tenantId, UserId = userId, Slug = tenant.Slug };
    }

    [HttpGet("{tenantId:guid}/domain")]
    public async Task<List<TenantDomainDto>> GetDomains(Guid tenantId)
    {
        var domains = await domainService.GetByTenantIdAsync(tenantId);
        return domains.Select(ToDto).ToList();
    }

    [HttpPost("{tenantId:guid}/domain")]
    public async Task<ActionResult<TenantDomainDto>> CreateDomain(Guid tenantId, TenantDomainCreateRequest request)
    {
        var domain = await domainService.CreateAsync(new AppTenantDomainCreateRequest
        {
            TenantId = tenantId,
            Name = request.Name
        });

        return StatusCode(StatusCodes.Status201Created, ToDto(domain));
    }

    // Reads the domain's DNS. Fails with an error if the TXT record is not there yet.
    [HttpPost("{tenantId:guid}/domain/{id:guid}/verify")]
    public async Task<TenantDomainDto> VerifyDomain(Guid tenantId, Guid id) =>
        ToDto(await domainService.VerifyAsync(tenantId, id));

    [HttpDelete("{tenantId:guid}/domain/{id:guid}")]
    public async Task<IActionResult> DeleteDomain(Guid tenantId, Guid id)
    {
        await domainService.DeleteAsync(tenantId, id);
        return NoContent();
    }

    private static TenantDomainDto ToDto(AppTenantDomainDto domain) => new()
    {
        Id = domain.Id,
        Name = domain.Name,
        TxtRecord = domain.TxtRecord,
        VerifiedAt = domain.VerifiedAt
    };
}
