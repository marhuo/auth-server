using AuthServer.Application.Roles.Services;
using AuthServer.Domain.Common;

namespace AuthServer.Web.Controllers.Api;

public static class RoleServiceExtensions
{
    // Resolves role names of the resource server to ids. Throws a ValidationException with the
    // given key when any of the names is not found.
    public static async Task<List<Guid>> GetRequiredIdsByNamesAsync(
        this RoleService roleService, Guid resourceServerId, IReadOnlyCollection<string> names, string key)
    {
        var idsByName = await roleService.GetIdsByNamesAsync(resourceServerId, names);

        var missing = idsByName.Where(p => p.Value is null).Select(p => p.Key).ToList();
        if (missing.Count > 0)
            throw new ValidationException(key, ErrorCode.NotFound,
                $"Unknown roles: {string.Join(", ", missing)}",
                $"Tuntemattomia rooleja: {string.Join(", ", missing)}");

        return idsByName.Values.Select(id => id!.Value).Distinct().ToList();
    }
}
