using AuthServer.Application.Common.Messages;
using AuthServer.Application.Scopes.Messages;
using AuthServer.Application.Scopes.Services;
using AuthServer.Application.Clients.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/scope")]
[ApiController]
public class ScopeController(ScopeService service) : AdminControllerBase
{
    [HttpGet]
    public Task<PagedListDto<ScopeDto>> GetPagedList([FromQuery] ScopePagedListRequest request) =>
        service.GetPagedListAsync(request);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScopeDto>> GetById(Guid id)
    {
        var scope = await service.GetByIdAsync(id);
        return scope is null ? NotFound() : scope;
    }

    [HttpPost("by-ids")]
    public Task<List<ScopeDto>> GetByIds([FromBody] Guid[] ids) =>
        service.GetByIdsAsync(ids);

    [HttpGet("selectable-for-client/{resourceServerId:guid}")]
    public Task<List<ScopeDto>> GetSelectableForClient(Guid resourceServerId, [BindRequired] ClientDtoTypes clientType) =>
        service.GetSelectableForClientAsync(resourceServerId, clientType);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ScopeCreateRequest request)
    {
        var id = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ScopeUpdateRequest request)
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
