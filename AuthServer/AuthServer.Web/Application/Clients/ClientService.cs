using AuthServer.Web.Persistence;
using AuthServer.Web.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Application.Clients;

public enum ClientAuthStatus { Success, Unknown, InvalidSecret }

public partial class ClientService(AuthDbContext db)
{
    public const int MinSecretLength = 32;
    public const int MaxDisplayNameLength = 200;

    [GeneratedRegex(@"^[a-zA-Z0-9][a-zA-Z0-9._-]{1,99}\z")]
    private static partial Regex ClientIdRegex();

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

        if (IsConfidentialType(entity.Type) && !VerifySecret(entity.SecretHash, secret))
            return (ClientAuthStatus.InvalidSecret, null);

        return (ClientAuthStatus.Success, ToClient(entity));
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

        var needsSecret = IsConfidentialType(type);
        var signsInUsers = SignsInUsersType(type);

        var errors = new ErrorCollector();

        if (!ClientIdRegex().IsMatch(clientId))
            errors.Add(nameof(clientId), ErrorCode.InvalidFormat,
                "Client id may contain only letters, digits, '.', '_' and '-' (2-100 characters).",
                "Client id saa sisältää vain kirjaimia, numeroita sekä merkit '.', '_' ja '-' (2–100 merkkiä).");

        if (displayName.Length is 0 or > MaxDisplayNameLength)
            errors.Add(nameof(displayName), ErrorCode.InvalidLength,
                $"Display name must be 1-{MaxDisplayNameLength} characters.",
                $"Näyttönimen on oltava 1–{MaxDisplayNameLength} merkkiä pitkä.");

        if (needsSecret && (secret is null || secret.Length < MinSecretLength))
            errors.Add(nameof(secret), ErrorCode.InvalidLength,
                $"Secret must be at least {MinSecretLength} characters.",
                $"Salaisuuden on oltava vähintään {MinSecretLength} merkkiä pitkä.");

        if (!needsSecret && secret is not null)
            errors.Add(nameof(secret), ErrorCode.NotAllowed,
                "A public client cannot have a secret.",
                "Julkisella clientilla ei voi olla salaisuutta.");

        if (signsInUsers && redirects.Count == 0)
            errors.Add(nameof(redirectUris), ErrorCode.Required,
                "At least one redirect URI is required.",
                "Vähintään yksi redirect-osoite vaaditaan.");

        if (type == ClientTypes.ConfidentialMachine)
        {
            if (tenantId is null)
                errors.Add(nameof(tenantId), ErrorCode.Required,
                    "A machine client must belong to a tenant.",
                    "Konetunnuksen on kuuluttava asiakkaalle.");

            if (ids.Count == 0)
                errors.Add(nameof(scopeIds), ErrorCode.Required,
                    "A machine client needs at least one scope.",
                    "Konetunnus tarvitsee vähintään yhden scopen.");

            if (redirects.Count > 0 || postLogouts.Count > 0)
                errors.Add(nameof(redirectUris), ErrorCode.NotAllowed,
                    "A machine client cannot have redirect URIs.",
                    "Konetunnuksella ei voi olla redirect-osoitteita.");
        }

        if (redirects.Concat(postLogouts).Any(uri => !IsValidRedirectUri(uri)))
            errors.Add(nameof(redirectUris), ErrorCode.InvalidFormat,
                "A redirect URI is not valid. Use https, or http for localhost only.",
                "Redirect-osoite ei kelpaa. Käytä https-osoitetta, tai http vain localhostille.");

        errors.ThrowIfAny();

        if (!await db.ResourceServers.AnyAsync(r => r.Id == resourceServerId))
            throw new ValidationException(
                Errors.NotFound(nameof(resourceServerId), "Resource server", "Resurssipalvelinta"));

        if (tenantId is { } tid && !await db.Tenants.AnyAsync(t => t.Id == tid))
            throw new ValidationException(Errors.NotFound(nameof(tenantId), "Tenant", "Asiakasta"));

        var scopes = await db.Scopes.Where(s => ids.Contains(s.Id)).ToListAsync();
        if (scopes.Count != ids.Count)
            throw new ValidationException(Errors.NotFound(nameof(scopeIds), "Scope", "Scopea"));

        if (await db.Clients.AnyAsync(c => c.ClientId == clientId))
            throw ClientIdTaken();

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
                throw ClientIdTaken();

            throw;
        }

        return entity.Id;
    }

    public async Task AddScopeAsync(Guid id, Guid scopeId)
    {
        var client = await db.Clients.Include(c => c.Scopes).SingleOrDefaultAsync(c => c.Id == id)
            ?? throw ClientNotFound(nameof(id));

        var scope = await db.Scopes.SingleOrDefaultAsync(s => s.Id == scopeId)
            ?? throw new ValidationException(Errors.NotFound(nameof(scopeId), "Scope", "Scopea"));

        if (client.Scopes.Any(s => s.Id == scopeId))
            return;

        client.Scopes.Add(scope);
        await db.SaveChangesAsync();
    }

    public async Task RemoveScopeAsync(Guid id, Guid scopeId)
    {
        var client = await db.Clients.Include(c => c.Scopes).SingleOrDefaultAsync(c => c.Id == id)
            ?? throw ClientNotFound(nameof(id));

        var scope = client.Scopes.SingleOrDefault(s => s.Id == scopeId);
        if (scope is null)
            return;

        if (client.Type == ClientTypes.ConfidentialMachine && client.Scopes.Count == 1)
            throw new ValidationException(nameof(scopeId), ErrorCode.NotAllowed,
                "A machine client needs at least one scope.",
                "Konetunnus tarvitsee vähintään yhden scopen.");

        client.Scopes.Remove(scope);
        await db.SaveChangesAsync();
    }

    public async Task ChangeSecretAsync(Guid id, string newSecret)
    {
        if (newSecret is null || newSecret.Length < MinSecretLength)
            throw new ValidationException(nameof(newSecret), ErrorCode.InvalidLength,
                $"Secret must be at least {MinSecretLength} characters.",
                $"Salaisuuden on oltava vähintään {MinSecretLength} merkkiä pitkä.");

        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw ClientNotFound(nameof(id));

        if (!IsConfidentialType(client.Type))
            throw new ValidationException(nameof(id), ErrorCode.NotAllowed,
                "A public client cannot have a secret.",
                "Julkisella clientilla ei voi olla salaisuutta.");

        client.SecretHash = HashSecret(newSecret);
        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id)
    {
        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw ClientNotFound(nameof(id));

        client.IsActive = false;
        await db.SaveChangesAsync();
    }

    public Task<Client?> GetByIdAsync(Guid id) =>
        db.Clients
            .Where(c => c.Id == id)
            .Select(ToClientExpression)
            .SingleOrDefaultAsync();

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

    private Task<ClientEntity?> LoadActiveAsync(string clientId) =>
        db.Clients.AsNoTracking().Include(c => c.Scopes)
            .SingleOrDefaultAsync(c => c.ClientId == clientId && c.IsActive);

    private static bool IsConfidentialType(ClientTypes type) =>
        type is ClientTypes.ConfidentialApplication or ClientTypes.ConfidentialMachine;

    private static bool SignsInUsersType(ClientTypes type) =>
        type is ClientTypes.PublicApplication or ClientTypes.ConfidentialApplication;

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

    private static bool IsValidRedirectUri(string value)
    {
        if (value.Contains('*') || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        if (!string.IsNullOrEmpty(uri.Fragment))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
    }

    private static ValidationException ClientIdTaken() =>
        new("clientId", ErrorCode.Taken, "Client id is taken.", "Client id on jo käytössä.");

    private static ValidationException ClientNotFound(string key) =>
        new(Errors.NotFound(key, "Client", "Clientia"));

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}