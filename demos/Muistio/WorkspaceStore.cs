using System.Collections.Concurrent;

namespace Muistio;

// Muistio's own data about a tenant: the name (the token only has the id) and the subscription.
// The auth server knows nothing about subscriptions.
public record Workspace(Guid TenantId, string Name, bool Paid, DateTimeOffset? PaidAt);

// Kept in memory only: restarting Muistio forgets everything, like it forgets the sign-ins
// (Program.cs). In development the auth server's database is recreated often, and old data here
// would refer to tenants that no longer exist.
public class WorkspaceStore
{
    private readonly ConcurrentDictionary<Guid, Workspace> _workspaces = new();

    public Workspace? Get(Guid tenantId) => _workspaces.GetValueOrDefault(tenantId);

    public void Save(Workspace workspace) => _workspaces[workspace.TenantId] = workspace;
}
