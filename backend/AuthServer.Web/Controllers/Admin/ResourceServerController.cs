using AuthServer.Application.Common.Messages;
using AuthServer.Application.ResourceServers.Messages;
using AuthServer.Application.ResourceServers.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/resource-server")]
public class ResourceServerController(ResourceServerService service) : AdminControllerBase
{
    [HttpGet]
    public Task<PagedListDto<ResourceServerDto>> GetPagedList([FromQuery] PagedListRequest request) =>
        service.GetPagedListAsync(request);

    [HttpGet("all")]
    public Task<List<ResourceServerDto>> GetAll() =>
        service.GetAllAsync();

    [HttpPost("by-ids")]
    public Task<List<ResourceServerDto>> GetByIds([FromBody] Guid[] ids) =>
        service.GetByIdsAsync(ids);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResourceServerDto>> GetById(Guid id)
    {
        var resourceServer = await service.GetByIdAsync(id);
        return resourceServer is null ? NotFound() : resourceServer;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ResourceServerCreateRequest request)
    {
        var id = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ResourceServerUpdateRequest request)
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
