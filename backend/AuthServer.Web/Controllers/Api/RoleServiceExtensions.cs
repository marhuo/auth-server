using AuthServer.Application.Roles.Services;
using AuthServer.Domain.Common;

namespace AuthServer.Web.Controllers.Api;

public static class RoleServiceExtensions
{
    // Resolves role names of any product to ids; auth server roles are never found. Which product role
    // is the right one (e.g. the admin role) is the caller's responsibility. Throws a
    // ValidationException with the given key when any of the names is not found.
    public static async Task<List<Guid>> GetRequiredProductRoleIdsAsync(
        this RoleService roleService, IReadOnlyCollection<string> names, string key)
    {
        var idsByName = await roleService.GetProductRoleIdsByNamesAsync(names);

        var missing = idsByName.Where(p => p.Value is null).Select(p => p.Key).ToList();
        if (missing.Count > 0)
            throw new ValidationException(key, ErrorCode.NotFound,
                $"Unknown roles: {string.Join(", ", missing)}",
                $"Tuntemattomia rooleja: {string.Join(", ", missing)}");

        return idsByName.Values.Select(id => id!.Value).Distinct().ToList();
    }
}
