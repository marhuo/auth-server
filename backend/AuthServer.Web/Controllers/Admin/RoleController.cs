using AuthServer.Application.Common.Messages;
using AuthServer.Application.Roles.Messages;
using AuthServer.Application.Roles.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/role")]
[ApiController]
public class RoleController(RoleService service) : AdminControllerBase
{
    [HttpGet]
    public Task<PagedListDto<RoleDto>> GetPagedList([FromQuery] RolePagedListRequest request) =>
        service.GetPagedListAsync(request);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id)
    {
        var role = await service.GetByIdAsync(id);
        return role is null ? NotFound() : role;
    }

    [HttpPost("by-ids")]
    public Task<List<RoleDto>> GetByIds([FromBody] Guid[] ids) =>
        service.GetByIdsAsync(ids);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] RoleCreateRequest request)
    {
        var id = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] RoleUpdateRequest request)
    {
        await service.UpdateAsync(request);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
