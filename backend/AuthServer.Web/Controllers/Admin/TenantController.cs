using AuthServer.Application.Common.Messages;
using AuthServer.Application.Tenants.Messages;
using AuthServer.Application.Tenants.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/tenant")]
public class TenantController(TenantService service) : AdminControllerBase
{
    [HttpGet]
    public Task<PagedListDto<TenantDto>> GetPagedList([FromQuery] PagedListRequest request) =>
        service.GetPagedListAsync(request);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetById(Guid id)
    {
        var tenant = await service.GetByIdAsync(id);
        return tenant is null ? NotFound() : tenant;
    }

    [HttpPost("by-ids")]
    public Task<List<TenantDto>> GetByIds([FromBody] Guid[] ids) =>
        service.GetByIdsAsync(ids);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] TenantCreateRequest request)
    {
        var id = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] TenantUpdateRequest request)
    {
        await service.UpdateAsync(request);
        return NoContent();
    }

    [HttpPost("suspend")]
    public async Task<IActionResult> Suspend([FromBody] TenantSuspendRequest request)
    {
        await service.SuspendAsync(request);
        return NoContent();
    }

    [HttpPost("{id:guid}/unsuspend")]
    public async Task<IActionResult> Unsuspend(Guid id)
    {
        await service.UnsuspendAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
