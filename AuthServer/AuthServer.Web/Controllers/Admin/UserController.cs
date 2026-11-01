using AuthServer.Web.Application.Users;
using AuthServer.Web.Dto.Admin.Users;
using AuthServer.Web.Dto.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[Route("api/admin/user")]
public class UserController(UserService service) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedListDto<UserDto>>> GetPagedList(
        [FromQuery] PagedListRequest request)
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

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(UserCreateRequest request)
    {
        var roles = request.UserRoles.Select(r => new UserRole
        {
            RoleId = r.RoleId,
            TenantId = r.TenantId
        });

        var id = await service.CreateAsync(request.UserName, request.Email, request.Password, roles);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("tenant-role/add")]
    public async Task<IActionResult> AddTenantRole(TenantRoleAddRequest request)
    {
        await service.AddTenantRoleAsync(request.UserId, request.TenantId, request.RoleId);
        return NoContent();
    }

    [HttpPost("tenant-role/remove")]
    public async Task<IActionResult> RemoveTenantRole(TenantRoleRemoveRequest request)
    {
        await service.RemoveTenantRoleAsync(request.UserId, request.TenantId, request.RoleId);
        return NoContent();
    }

    [HttpPost("auth-server-role/add")]
    public async Task<IActionResult> AddAuthServerRole(AuthServerRoleAddRequest request)
    {
        await service.AddAuthServerRoleAsync(request.UserId, request.RoleId);
        return NoContent();
    }

    [HttpPost("auth-server-role/remove")]
    public async Task<IActionResult> RemoveAuthServerRole(AuthServerRoleRemoveRequest request)
    {
        await service.RemoveAuthServerRoleAsync(request.UserId, request.RoleId);
        return NoContent();
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        IsActive = user.IsActive,
        LockedUntil = user.LockedUntil,
        UserRoles = user.UserRoles.Select(ToDto).ToList()
    };

    private static UserRoleDto ToDto(UserRole userRole) => new()
    {
        RoleId = userRole.RoleId,
        TenantId = userRole.TenantId
    };
}