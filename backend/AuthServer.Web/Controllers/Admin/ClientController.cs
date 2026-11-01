using AuthServer.Application.Clients.Messages;
using AuthServer.Application.Clients.Services;
using AuthServer.Application.Common.Messages;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/client")]
[ApiController]
public class ClientController(ClientService service) : AdminControllerBase
{
    [HttpGet]
    public Task<PagedListDto<ClientDto>> GetPagedList([FromQuery] PagedListRequest request) =>
        service.GetPagedListAsync(request);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id)
    {
        var client = await service.GetByIdAsync(id);
        return client is null ? NotFound() : client;
    }

    [HttpPost("by-ids")]
    public Task<List<ClientDto>> GetByIds([FromBody] Guid[] ids) =>
        service.GetByIdsAsync(ids);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ClientCreateRequest request)
    {
        var id = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ClientUpdateRequest request)
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

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await service.DeactivateAsync(id);
        return NoContent();
    }

    [HttpPost("change-secret")]
    public async Task<IActionResult> ChangeSecret([FromBody] ClientChangeSecretRequest request)
    {
        await service.ChangeSecretAsync(request);
        return NoContent();
    }

    [HttpPost("scope/add")]
    public async Task<IActionResult> AddScope([FromBody] ClientAddScopeRequest request)
    {
        await service.AddScopeAsync(request);
        return NoContent();
    }

    [HttpPost("scope/remove")]
    public async Task<IActionResult> RemoveScope([FromBody] ClientRemoveScopeRequest request)
    {
        await service.RemoveScopeAsync(request);
        return NoContent();
    }

    [HttpPost("redirect-uri/add")]
    public async Task<IActionResult> AddRedirectUri([FromBody] ClientAddRedirectUriRequest request)
    {
        await service.AddRedirectUriAsync(request);
        return NoContent();
    }

    [HttpPost("redirect-uri/remove")]
    public async Task<IActionResult> RemoveRedirectUri([FromBody] ClientRemoveRedirectUriRequest request)
    {
        await service.RemoveRedirectUriAsync(request);
        return NoContent();
    }

    [HttpPost("post-logout-redirect-uri/add")]
    public async Task<IActionResult> AddPostLogoutRedirectUri([FromBody] ClientAddPostLogoutRedirectUriRequest request)
    {
        await service.AddPostLogoutRedirectUriAsync(request);
        return NoContent();
    }

    [HttpPost("post-logout-redirect-uri/remove")]
    public async Task<IActionResult> RemovePostLogoutRedirectUri([FromBody] ClientRemovePostLogoutRedirectUriRequest request)
    {
        await service.RemovePostLogoutRedirectUriAsync(request);
        return NoContent();
    }
}
