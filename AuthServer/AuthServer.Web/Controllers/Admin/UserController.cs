using AuthServer.Web.Application.Users;
using AuthServer.Web.Dto.Admin.Users;
using AuthServer.Web.Dto.Shared;
using AuthServer.Web.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/user")]
public class UserController(UserService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<UserDto>>> GetPagedList([FromQuery] PagedListRequest request)
    {
        var page = await service.GetPagedListAsync(request.SearchTerm, request.PageNumber, request.PageSize);
        return page.ToDto(ToDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        return user is null ? NotFound() : ToDto(user);
    }

    [HttpPost("by-ids")]
    public async Task<ActionResult<List<UserDto>>> GetByIds([FromBody] Guid[] ids)
    {
        var users = await service.GetByIdsAsync(ids);
        return users.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(UserCreateRequest request)
    {
        var id = await service.CreateAsync(request.UserName, request.Email, request.Password, []);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("role/add")]
    public async Task<IActionResult> AddRole(AddUserRoleRequest request)
    {
        if (await service.HasRoleAsync(request.UserId, request.RoleId, request.TenantId))
        {
            throw new ValidationException("", ErrorCode.NotAllowed, "User already has this role.", "Käyttäjällä on jo tämä rooli.");
        }
        await service.AddRoleAsync(request.UserId, request.RoleId, request.TenantId);
        return NoContent();
    }

    [HttpPost("role/remove")]
    public async Task<IActionResult> RemoveRole(RemoveUserRoleRequest request)
    {
        await service.RemoveRoleAsync(request.UserId, request.RoleId, request.TenantId);
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Update(UserUpdateRequest request)
    {
        await service.UpdateAsync(request.Id, request.UserName, request.Email, request.LockedUntil);
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(UserChangePasswordRequest request)
    {
        await service.ChangePasswordAsync(request.Id, request.NewPassword);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        LockedUntil = user.LockedUntil,
        CreatedAt = user.CreatedAt,
        UserRoles = user.UserRoles.Select(ToDto).ToList()
    };

    private static UserRoleDto ToDto(UserRole userRole) => new()
    {
        RoleId = userRole.RoleId,
        TenantId = userRole.TenantId,
        IsAuthServerRole = userRole.IsAuthServerRole
    };
}