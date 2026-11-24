namespace AuthServer.Domain.Scopes;

public interface IScopeRepository
{
    Task<Scope?> GetByIdAsync(Guid id);

    // Ordered by name.
    Task<List<Scope>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by name. SearchTerm matches the name and the display name.
    Task<PagedList<Scope>> GetPagedListAsync(ScopePagedListQuery query);

    // Ordered by name.
    Task<List<Scope>> GetByResourceServerIdAsync(Guid resourceServerId);

    // Scopes that are not tied to a resource server. Ordered by name.
    Task<List<Scope>> GetGlobalAsync();

    // Ids of the scopes that exist; unknown names are ignored.
    Task<List<Guid>> GetIdsByNamesAsync(IReadOnlyCollection<string> names);

    // Distinct audiences of the resource servers the given scopes belong to.
    Task<List<string>> GetAudiencesAsync(IReadOnlyCollection<string> names);

    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    Task<bool> IsAssignedToClientsAsync(Guid scopeId);

    Task<bool> AnyForResourceServerAsync(Guid resourceServerId);

    Task CreateAsync(Scope scope);

    Task UpdateAsync(Scope scope);

    Task DeleteAsync(Guid id);
}
