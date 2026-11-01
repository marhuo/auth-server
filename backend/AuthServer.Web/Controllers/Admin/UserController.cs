using AuthServer.Application.Common.Messages;
using AuthServer.Application.Users.Messages;
using AuthServer.Application.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/user")]
public class UserController(UserService service) : AdminControllerBase
{
    [HttpGet]
    public Task<PagedListDto<UserDto>> GetPagedList([FromQuery] UserPagedListRequest request) =>
        service.GetPagedListAsync(request);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        return user is null ? NotFound() : user;
    }

    [HttpPost("by-ids")]
    public Task<List<UserDto>> GetByIds([FromBody] Guid[] ids) =>
        service.GetByIdsAsync(ids);

    // Users are created only by invitation. Creates the user and sends an invitation link, or adds
    // the roles to an existing user (no invitation then). Returns the id of the user, existing or
    // new, therefore 200 instead of 201.
    [HttpPost("invite")]
    public Task<Guid> Invite([FromBody] UserInviteRequest request) =>
        service.InviteAsync(request);

    [HttpPost("{id:guid}/resend-invite")]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        await service.ResendInviteAsync(id);
        return NoContent();
    }

    [HttpPost("role/add")]
    public async Task<IActionResult> AddRole([FromBody] AddUserRoleRequest request)
    {
        await service.AddRoleAsync(request);
        return NoContent();
    }

    [HttpPost("role/remove")]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveUserRoleRequest request)
    {
        await service.RemoveRoleAsync(request);
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UserUpdateRequest request)
    {
        await service.UpdateAsync(request);
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordRequest request)
    {
        await service.ChangePasswordAsync(request);
        return NoContent();
    }
}
