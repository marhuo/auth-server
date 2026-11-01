using AuthServer.Web.Application.ResourceServers;
using AuthServer.Web.Dto.Admin.ResourceServers;
using AuthServer.Web.Dto.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/resource-server")]
public class ResourceServerController(ResourceServerService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<ResourceServerDto>>> GetPagedList(
        [FromQuery] PagedListRequest request)
    {
        var page = await service.GetPagedListAsync(request.SearchTerm, request.PageNumber, request.PageSize);
        return page.ToDto(ToDto);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<ResourceServerDto>>> GetAll()
    {
        var resourceServers = await service.GetAllAsync();
        return resourceServers.Select(ToDto).ToList();
    }

    [HttpGet("by-ids")]
    public async Task<ActionResult<List<ResourceServerDto>>> GetByIds([FromQuery] Guid[] ids)
    {
        var resourceServers = await service.GetByIdsAsync(ids);
        return resourceServers.Select(ToDto).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResourceServerDto>> GetById(Guid id)
    {
        var resourceServer = await service.GetByIdAsync(id);
        return resourceServer is null ? NotFound() : ToDto(resourceServer);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(ResourceServerCreateRequest request)
    {
        var id = await service.CreateAsync(request.Audience, request.DisplayName);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update(ResourceServerUpdateRequest request)
    {
        await service.UpdateAsync(request.Id, request.DisplayName);
        return NoContent();
    }

    private static ResourceServerDto ToDto(ResourceServer rs) => new()
    {
        Id = rs.Id,
        Audience = rs.Audience,
        DisplayName = rs.DisplayName
    };
}