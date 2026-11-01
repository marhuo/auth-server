using AuthServer.Web.Application.Roles;
using AuthServer.Web.Dto.Admin.Roles;
using AuthServer.Web.Dto.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/role")]
[ApiController]
public class RoleController(RoleService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<RoleDto>>> GetPagedList([FromQuery] RolePagedListRequest request)
    {
        var pagedList = await service.GetPagedListAsync(request.SearchTerm, request.ResourceServerId, request.PageNumber, request.PageSize);
        return pagedList.ToDto(ToDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto?>> GetById(Guid id)
    {
        var role = await service.GetByIdAsync(id);
        if (role is null) return NotFound();
        return ToDto(role);
    }

    [HttpPost("by-ids")]
    public async Task<ActionResult<List<RoleDto>>> GetByIds([FromBody] Guid[] ids)
    {
        var roles = await service.GetByIdsAsync(ids);
        return roles.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] RoleCreateRequest request)
    {
        var id = await service.CreateAsync(request.Name, request.Description, request.ResourceServerId);
        return id;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] RoleUpdateRequest request)
    {
        await service.UpdateAsync(request.Id, request.Name, request.Description, request.ResourceServerId);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    private static RoleDto ToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        ResourceServerId = role.ResourceServerId,
        CreatedAt = role.CreatedAt
    };
}
