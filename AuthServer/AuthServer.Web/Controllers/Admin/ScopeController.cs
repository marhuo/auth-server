using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Scopes;
using AuthServer.Web.Dto.Admin.Clients;
using AuthServer.Web.Dto.Admin.Scopes;
using AuthServer.Web.Dto.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/scope")]
[ApiController]
public class ScopeController(ScopeService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<ScopeDto>>> GetPagedList([FromQuery] ScopePagedListRequest request)
    {
        var page = await service.GetPagedListAsync(request.SearchTerm, request.ResourceServerId, request.PageNumber, request.PageSize);
        return page.ToDto(ToDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScopeDto>> GetById(Guid id)
    {
        var scope = await service.GetByIdAsync(id);
        if (scope is null) return NotFound();
        return ToDto(scope);
    }

    [HttpPost("by-ids")]
    public async Task<ActionResult<List<ScopeDto>>> GetByIds([FromBody] Guid[] ids)
    {
        var scopes = await service.GetByIdsAsync(ids);
        return scopes.Select(ToDto).ToList();
    }

    [HttpGet("selectable-for-client/{resourceServerId:guid}")]
    public async Task<ActionResult<List<ScopeDto>>> GetSelectableForClient(Guid resourceServerId, [BindRequired] ClientDtoTypes clientType)
    {
        var serverScopes = await service.GetByResourceServerIdAsync(resourceServerId);
        if (clientType.ToDomain().SignsInUsers)
        {
            var globalScopes = await service.GetGlobalAsync();
            return globalScopes.Concat(serverScopes).Select(ToDto).ToList();
        }
        return serverScopes.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ScopeCreateRequest request)
    {
        var id = await service.CreateAsync(request.Name, request.DisplayName, request.ResourceServerId);
        return id;
    }
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ScopeUpdateRequest request)
    {
        await service.UpdateAsync(request.Id, request.Name, request.DisplayName, request.ResourceServerId);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    private static ScopeDto ToDto(Scope scope) => new()
    {
        Id = scope.Id,
        Name = scope.Name,
        DisplayName = scope.DisplayName,
        ResourceServerId = scope.ResourceServerId,
        CreatedAt = scope.CreatedAt,
        IsStandard = scope.IsStandard,
    };
}
