using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Web.Application.Clients;

public enum ClientAuthStatus { Success, Unknown, InvalidSecret }

public class ClientService(AuthDbContext db, ClientValidator validator)
{
    private static readonly Expression<Func<ClientEntity, Client>> ToClientExpression = e => new Client
    {
        Id = e.Id,
        Type = e.Type,
        ClientId = e.ClientId,
        DisplayName = e.DisplayName,
        IsActive = e.IsActive,
        ResourceServerId = e.ResourceServerId,
        CreatedAt = e.CreatedAt,
        TenantId = e.TenantId,
        RedirectUris = e.RedirectUris,
        PostLogoutRedirectUris = e.PostLogoutRedirectUris,
        Scopes = e.Scopes.Select(s => s.Id).ToList()
    };

    private static readonly Func<ClientEntity, Client> ToClient = ToClientExpression.Compile();

    public static string GenerateSecret() => Base64Url(RandomNumberGenerator.GetBytes(32));

    public async Task<(ClientAuthStatus Status, Client? Client)> AuthenticateAsync(string? clientId, string? secret)
    {
        if (string.IsNullOrEmpty(clientId))
            return (ClientAuthStatus.Unknown, null);

        var entity = await LoadActiveAsync(clientId);
        if (entity is null)
            return (ClientAuthStatus.Unknown, null);

        var client = ToClient(entity);

        if (client.IsConfidential && !VerifySecret(entity.SecretHash, secret))
            return (ClientAuthStatus.InvalidSecret, null);

        return (ClientAuthStatus.Success, client);
    }

    public async Task<Client?> FindActiveAsync(string clientId)
    {
        var entity = await LoadActiveAsync(clientId);
        return entity is null ? null : ToClient(entity);
    }

    public Task<bool> IsPostLogoutRedirectUriRegisteredAsync(string uri) =>
        db.Clients.AnyAsync(c => c.IsActive && c.PostLogoutRedirectUris.Contains(uri));

    public async Task<Guid> CreateAsync(
        ClientTypes type,
        string clientId,
        string displayName,
        string? secret,
        Guid resourceServerId,
        Guid? tenantId,
        IEnumerable<string>? redirectUris,
        IEnumerable<string>? postLogoutRedirectUris,
        IEnumerable<Guid>? scopeIds)
    {
        clientId = clientId?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        var redirects = Clean(redirectUris);
        var postLogouts = Clean(postLogoutRedirectUris);
        var ids = (scopeIds ?? []).Distinct().ToList();

        await validator.ValidateCreateAsync(
            type, clientId, displayName, secret, resourceServerId, tenantId, redirects, postLogouts, ids);

        var scopes = await db.Scopes.Where(s => ids.Contains(s.Id)).ToListAsync();

        var entity = new ClientEntity
        {
            Id = Guid.CreateVersion7(),
            ClientId = clientId,
            DisplayName = displayName,
            Type = type,
            SecretHash = secret is null ? null : HashSecret(secret),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId,
            RedirectUris = redirects,
            PostLogoutRedirectUris = postLogouts,
            ResourceServerId = resourceServerId,
            Scopes = scopes
        };

        db.Clients.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Clients.AnyAsync(c => c.ClientId == clientId))
                throw ClientValidator.ClientIdTaken();

            throw;
        }

        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, string clientId, string displayName, Guid? tenantId)
    {
        clientId = clientId?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        var client = await FindAsync(id);

        await validator.ValidateUpdateAsync(client, clientId, displayName, tenantId);

        client.ClientId = clientId;
        client.DisplayName = displayName;
        client.TenantId = tenantId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Clients.AnyAsync(c => c.ClientId == clientId && c.Id != id))
                throw ClientValidator.ClientIdTaken();

            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await FindAsync(id);

        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.RefreshTokens.Where(t => t.ClientId == id).ExecuteDeleteAsync();
        await db.AuthorizationCodes.Where(c => c.ClientId == id).ExecuteDeleteAsync();

        client.Scopes.Clear();
        db.Clients.Remove(client);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task AddScopeAsync(Guid id, Guid scopeId)
    {
        var client = await FindAsync(id);

        await validator.ValidateAddScopeAsync(client, scopeId);

        if (client.Scopes.Any(s => s.Id == scopeId))
            return;

        var scope = await db.Scopes.SingleAsync(s => s.Id == scopeId);

        client.Scopes.Add(scope);
        await db.SaveChangesAsync();
    }

    public async Task RemoveScopeAsync(Guid id, Guid scopeId)
    {
        var client = await FindAsync(id);

        var scope = client.Scopes.SingleOrDefault(s => s.Id == scopeId);
        if (scope is null)
            return;

        validator.ValidateRemoveScope(client);

        client.Scopes.Remove(scope);
        await db.SaveChangesAsync();
    }

    public async Task AddRedirectUriAsync(Guid id, string uri)
    {
        uri = uri?.Trim() ?? "";

        var client = await FindAsync(id);

        validator.ValidateAddUri(client, uri, client.RedirectUris);

        client.RedirectUris = [.. client.RedirectUris, uri];
        await db.SaveChangesAsync();
    }


    public async Task RemoveRedirectUriAsync(Guid id, string uri)
    {
        uri = uri?.Trim() ?? "";

        var client = await FindAsync(id);

        if (!client.RedirectUris.Contains(uri))
            return;

        validator.ValidateRemoveRedirectUri(client);

        client.RedirectUris = client.RedirectUris.Where(u => u != uri).ToList();
        await db.SaveChangesAsync();
    }

    public async Task AddPostLogoutRedirectUriAsync(Guid id, string uri)
    {
        uri = uri?.Trim() ?? "";

        var client = await FindAsync(id);

        validator.ValidateAddUri(client, uri, client.PostLogoutRedirectUris);

        client.PostLogoutRedirectUris = [.. client.PostLogoutRedirectUris, uri];
        await db.SaveChangesAsync();
    }

    public async Task RemovePostLogoutRedirectUriAsync(Guid id, string uri)
    {
        uri = uri?.Trim() ?? "";

        var client = await FindAsync(id);

        if (!client.PostLogoutRedirectUris.Contains(uri))
            return;

        client.PostLogoutRedirectUris = client.PostLogoutRedirectUris.Where(u => u != uri).ToList();
        await db.SaveChangesAsync();
    }

    public async Task ChangeSecretAsync(Guid id, string newSecret)
    {
        newSecret ??= "";

        var client = await FindAsync(id);

        validator.ValidateChangeSecret(client, newSecret);

        client.SecretHash = HashSecret(newSecret);
        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id)
    {
        var client = await FindAsync(id);

        client.IsActive = false;
        await db.SaveChangesAsync();
    }

    public Task<Client?> GetByIdAsync(Guid id) =>
        db.Clients
            .Where(c => c.Id == id)
            .Select(ToClientExpression)
            .SingleOrDefaultAsync();

    public Task<Client?> GetByClientIdAsync(string clientId) =>
        db.Clients
            .Where(c => c.ClientId == clientId)
            .Select(ToClientExpression)
            .SingleOrDefaultAsync();

    public async Task<List<Client>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        return await db.Clients
            .Where(c => distinctIds.Contains(c.Id))
            .OrderBy(c => c.ClientId)
            .Select(ToClientExpression)
            .ToListAsync();
    }

    public Task<Guid?> GetIdByClientIdAsync(string clientId) =>
        db.Clients
            .Where(c => c.ClientId == clientId)
            .Select(c => (Guid?)c.Id)
            .SingleOrDefaultAsync();

    public Task<PagedList<Client>> GetPagedListAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var clients = db.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{EscapeLike(searchTerm.Trim())}%";
            clients = clients.Where(c =>
                EF.Functions.ILike(c.ClientId, pattern) ||
                EF.Functions.ILike(c.DisplayName, pattern));
        }

        var page = clients
            .OrderBy(c => c.ClientId)
            .Select(ToClientExpression)
            .ToPagedList(pageNumber, pageSize);

        return Task.FromResult(page);
    }

    private async Task<ClientEntity> FindAsync(Guid id) =>
        await db.Clients.Include(c => c.Scopes).SingleOrDefaultAsync(c => c.Id == id)
        ?? throw new ValidationException(Errors.NotFound(nameof(id), "Client", "Clientia"));

    private Task<ClientEntity?> LoadActiveAsync(string clientId) =>
        db.Clients.AsNoTracking().Include(c => c.Scopes)
            .SingleOrDefaultAsync(c => c.ClientId == clientId && c.IsActive);

    private static bool VerifySecret(string? secretHash, string? secret)
    {
        if (secretHash is null || string.IsNullOrEmpty(secret))
            return false;

        var computed = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        var stored = Convert.FromHexString(secretHash);

        return CryptographicOperations.FixedTimeEquals(computed, stored);
    }

    private static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static List<string> Clean(IEnumerable<string>? values) =>
        (values ?? [])
            .Select(v => v?.Trim() ?? "")
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}