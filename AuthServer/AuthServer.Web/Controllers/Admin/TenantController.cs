using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Dto.Admin.Tenants;
using AuthServer.Web.Dto.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/tenant")]
public class TenantController(TenantService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<TenantDto>>> GetPagedList(
        [FromQuery] PagedListRequest request)
    {
        var page = await service.GetPagedListAsync(request.SearchTerm, request.PageNumber, request.PageSize);
        return page.ToDto(ToDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetById(Guid id)
    {
        var tenant = await service.GetByIdAsync(id);
        return tenant is null ? NotFound() : ToDto(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(TenantCreateRequest request)
    {
        var id = await service.CreateAsync(request.Name, request.Slug);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update(TenantUpdateRequest request)
    {
        await service.UpdateAsync(request.Id, request.Name, request.Slug);
        return NoContent();
    }

    [HttpPost("suspend")]
    public async Task<IActionResult> Suspend(TenantSuspendRequest request)
    {
        await service.SuspendAsync(request.Id, request.SuspensionReason);
        return NoContent();
    }

    [HttpPost("{id:guid}/unsuspend")]
    public async Task<IActionResult> Unsuspend(Guid id)
    {
        await service.UnsuspendAsync(id);
        return NoContent();
    }

    private static TenantDto ToDto(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Slug = tenant.Slug,
        SuspendedAt = tenant.SuspendedAt,
        SuspensionReason = tenant.SuspensionReason,
        CreatedAt = tenant.CreatedAt
    };
}