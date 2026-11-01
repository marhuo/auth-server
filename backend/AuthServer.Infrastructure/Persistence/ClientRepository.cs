using AuthServer.Domain.Clients;
using AuthServer.Domain.Common;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class ClientRepository(AuthDbContext db) : IClientRepository
{
    private IQueryable<ClientEntity> ReadQuery => db.Clients.AsNoTracking().Include(c => c.Scopes);

    public async Task<Client?> GetByIdAsync(Guid id)
    {
        var entity = await ReadQuery.SingleOrDefaultAsync(c => c.Id == id);
        return entity?.ToDomain();
    }

    public async Task<Client?> GetByClientIdAsync(string clientId)
    {
        var entity = await ReadQuery.SingleOrDefaultAsync(c => c.ClientId == clientId);
        return entity?.ToDomain();
    }

    public async Task<Client?> FindActiveByClientIdAsync(string clientId)
    {
        var entity = await ReadQuery.SingleOrDefaultAsync(c => c.ClientId == clientId && c.IsActive);
        return entity?.ToDomain();
    }

    public async Task<List<Client>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        var entities = await ReadQuery
            .Where(c => distinctIds.Contains(c.Id))
            .OrderBy(c => c.ClientId)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<PagedList<Client>> GetPagedListAsync(PagedListQuery query)
    {
        var clients = ReadQuery;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = query.SearchTerm.ToContainsPattern();
            clients = clients.Where(c =>
                EF.Functions.ILike(c.ClientId, pattern) ||
                EF.Functions.ILike(c.DisplayName, pattern));
        }

        return clients.OrderBy(c => c.ClientId).ToPagedListAsync(query, e => e.ToDomain());
    }

    public Task<bool> IsPostLogoutRedirectUriRegisteredAsync(string uri) =>
        db.Clients.AnyAsync(c => c.IsActive && c.PostLogoutRedirectUris.Contains(uri));

    public Task<bool> ClientIdExistsAsync(string clientId, Guid? excludeId = null) =>
        db.Clients.AnyAsync(c => c.ClientId == clientId && c.Id != excludeId);

    public Task<bool> AnyForResourceServerAsync(Guid resourceServerId) =>
        db.Clients.AnyAsync(c => c.ResourceServerId == resourceServerId);

    public Task<bool> AnyForTenantAsync(Guid tenantId) =>
        db.Clients.AnyAsync(c => c.TenantId == tenantId);

    public async Task CreateAsync(Client client)
    {
        var entity = ClientEntity.FromDomain(client);
        await SyncScopesAsync(entity, client.ScopeIds);

        db.Clients.Add(entity);
        await SaveAsync();
    }

    public async Task UpdateAsync(Client client)
    {
        var entity = await db.Clients.Include(c => c.Scopes).SingleAsync(c => c.Id == client.Id);
        var becomesDeleted = entity.DeletedAt is null && client.Deleted;

        entity.UpdateFrom(client);
        await SyncScopesAsync(entity, client.ScopeIds);

        if (!becomesDeleted)
        {
            await SaveAsync();
            return;
        }

        // Tokens and codes of a deleted client are removed together with the soft delete.
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.RefreshTokens.IgnoreQueryFilters().Where(t => t.ClientId == client.Id).ExecuteDeleteAsync();
        await db.AuthorizationCodes.IgnoreQueryFilters().Where(c => c.ClientId == client.Id).ExecuteDeleteAsync();
        await SaveAsync();

        await transaction.CommitAsync();
    }

    private async Task SyncScopesAsync(ClientEntity entity, IReadOnlyList<Guid> scopeIds)
    {
        entity.Scopes.RemoveAll(s => !scopeIds.Contains(s.Id));

        var missing = scopeIds.Where(id => entity.Scopes.All(s => s.Id != id)).ToList();
        if (missing.Count == 0)
            return;

        var added = await db.Scopes.Where(s => missing.Contains(s.Id)).ToListAsync();
        entity.Scopes.AddRange(added);
    }

    private async Task SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AuthDbContext.ClientIdIndex
        })
        {
            // A concurrent request took the client id after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(Client.ClientIdTakenError());
        }
    }
}
