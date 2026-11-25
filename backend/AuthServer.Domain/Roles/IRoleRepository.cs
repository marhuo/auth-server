namespace AuthServer.Domain.Roles;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);

    // Ordered by name.
    Task<List<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    Task<PagedList<Role>> GetPagedListAsync(RolePagedListQuery query);

    // Roles of the products (any resource server except the auth server); role names are unique
    // across resource servers. Keys are role names. Names must be normalized with Role.NormalizeName.
    Task<Dictionary<string, Guid>> GetProductRoleIdsByNamesAsync(IReadOnlyCollection<string> names);

    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    Task<bool> IsAssignedToUsersAsync(Guid roleId);

    Task<bool> AnyForResourceServerAsync(Guid resourceServerId);

    Task<List<Guid>> GetIdsByResourceServerAsync(Guid resourceServerId);


    // Names of the user's auth server roles.
    Task<List<string>> GetAuthServerRoleNamesAsync(Guid userId);


    // Ids of the auth server roles the user holds.
    Task<List<Guid>> GetAuthServerRoleIdsAsync(Guid userId);


    Task CreateAsync(Role role);

    Task UpdateAsync(Role role);

    Task DeleteAsync(Guid id);
}
