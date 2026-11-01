using AuthServer.Web.Application.Clients;
using AuthServer.Web.Dto.Admin.Clients;
using AuthServer.Web.Dto.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/client")]
[ApiController]
public class ClientController(ClientService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<ClientDto>>> GetPagedList([FromQuery] PagedListRequest request)
    {
        var pagedList = await service.GetPagedListAsync(request.SearchTerm, request.PageNumber, request.PageSize);
        return pagedList.ToDto(ToDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto?>> GetById(Guid id)
    {
        var client = await service.GetByIdAsync(id);
        if (client is null) return NotFound();
        return ToDto(client);
    }

    [HttpPost("by-ids")]
    public async Task<ActionResult<List<ClientDto>>> GetByIds([FromBody] Guid[] ids)
    {
        var clients = await service.GetByIdsAsync(ids);
        return clients.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ClientCreateRequest request)
    {
        var id = await service.CreateAsync(
            request.Type.ToDomain(),
            request.ClientId,
            request.DisplayName,
            request.Secret,
            request.ResourceServerId,
            request.TenantId,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.ScopeIds);

        return id;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ClientUpdateRequest request)
    {
        await service.UpdateAsync(request.Id, request.ClientId, request.DisplayName, request.TenantId);
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
        await service.ChangeSecretAsync(request.Id, request.NewSecret);
        return NoContent();
    }

    [HttpPost("scope/add")]
    public async Task<IActionResult> AddScope([FromBody] ClientAddScopeRequest request)
    {
        await service.AddScopeAsync(request.Id, request.ScopeId);
        return NoContent();
    }

    [HttpPost("scope/remove")]
    public async Task<IActionResult> RemoveScope([FromBody] ClientRemoveScopeRequest request)
    {
        await service.RemoveScopeAsync(request.Id, request.ScopeId);
        return NoContent();
    }

    [HttpPost("redirect-uri/add")]
    public async Task<IActionResult> AddRedirectUri([FromBody] ClientAddRedirectUriRequest request)
    {
        await service.AddRedirectUriAsync(request.Id, request.Uri);
        return NoContent();
    }

    [HttpPost("redirect-uri/remove")]
    public async Task<IActionResult> RemoveRedirectUri([FromBody] ClientRemoveRedirectUriRequest request)
    {
        await service.RemoveRedirectUriAsync(request.Id, request.Uri);
        return NoContent();
    }

    [HttpPost("post-logout-redirect-uri/add")]
    public async Task<IActionResult> AddPostLogoutRedirectUri([FromBody] ClientAddPostLogoutRedirectUriRequest request)
    {
        await service.AddPostLogoutRedirectUriAsync(request.Id, request.Uri);
        return NoContent();
    }

    [HttpPost("post-logout-redirect-uri/remove")]
    public async Task<IActionResult> RemovePostLogoutRedirectUri([FromBody] ClientRemovePostLogoutRedirectUriRequest request)
    {
        await service.RemovePostLogoutRedirectUriAsync(request.Id, request.Uri);
        return NoContent();
    }

    private static ClientDto ToDto(Client client) => new()
    {
        Id = client.Id,
        Type = client.Type.ToDto(),
        ClientId = client.ClientId,
        DisplayName = client.DisplayName,
        IsActive = client.IsActive,
        ResourceServerId = client.ResourceServerId,
        CreatedAt = client.CreatedAt,
        TenantId = client.TenantId,
        RedirectUris = client.RedirectUris,
        PostLogoutRedirectUris = client.PostLogoutRedirectUris,
        ScopeIds = client.Scopes,
        AllowedGrantTypes = [.. client.GrantTypes]
    };
}