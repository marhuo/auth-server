using AuthServer.Domain.Clients;
using AuthServer.Domain.Tenants;
using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Seed;

public static class Extensions
{
    public static async Task<Guid> CreatePublicApplicationClientIfNotExistsAsync(this AuthDbContext db, string clientId, string displayName, Guid? tenantId, Guid targetResourceServerId, IEnumerable<string> redirectUris, IEnumerable<string> postLogoutRedirectUris, IEnumerable<Guid> allowedScopeIds, string? initiateLoginUri = null)
    {
        var existingClientId = await db.Clients
            .Where(i => i.ClientId == clientId)
            .Select(i => (Guid?)i.Id)
            .SingleOrDefaultAsync();

        if (existingClientId is not null)
        {
            return existingClientId.Value;
        }

        var id = Guid.CreateVersion7();
        var scopeEntities = allowedScopeIds.Any() ? await db.Scopes.Where(s => allowedScopeIds.Contains(s.Id)).ToListAsync() : [];
        var clientEntity = new ClientEntity
        {
            Id = id,
            ClientId = clientId,
            DisplayName = displayName,
            Type = ClientTypes.PublicApplication,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId,
            RedirectUris = redirectUris.ToList(),
            PostLogoutRedirectUris = postLogoutRedirectUris.ToList(),
            InitiateLoginUri = initiateLoginUri,
            TargetResourceServerId = targetResourceServerId,
            Scopes = scopeEntities,
            AccessTokenLifetime = Client.DefaultAccessTokenLifetime,
            IdTokenLifetime = Client.DefaultIdTokenLifetime,
            RefreshTokenLifetime = Client.DefaultRefreshTokenLifetime
        };

        db.Clients.Add(clientEntity);
        await db.SaveChangesAsync();
        return id;
    }

    // Built through Client.Create so that the secret is hashed the same way as in the app.
    public static async Task<Guid> CreateConfidentialClientIfNotExistsAsync(this AuthDbContext db, ClientTypes type, string clientId, string displayName, string secret, Guid targetResourceServerId, IEnumerable<string> redirectUris, IEnumerable<string> postLogoutRedirectUris, IEnumerable<Guid> allowedScopeIds, string? initiateLoginUri = null)
    {
        var existingClientId = await db.Clients
            .Where(i => i.ClientId == clientId)
            .Select(i => (Guid?)i.Id)
            .SingleOrDefaultAsync();

        if (existingClientId is not null)
        {
            return existingClientId.Value;
        }

        var signsInUsers = type.SignsInUsers;
        var client = Client.Create(
            type: type,
            clientId: clientId,
            displayName: displayName,
            secret: secret,
            targetResourceServerId: targetResourceServerId,
            tenantId: null,
            redirectUris: redirectUris,
            postLogoutRedirectUris: postLogoutRedirectUris,
            scopeIds: allowedScopeIds,
            accessTokenLifetime: Client.DefaultAccessTokenLifetime,
            idTokenLifetime: signsInUsers ? Client.DefaultIdTokenLifetime : null,
            refreshTokenLifetime: signsInUsers ? Client.DefaultRefreshTokenLifetime : null,
            now: DateTimeOffset.UtcNow);

        client.SetInitiateLoginUri(initiateLoginUri);

        var entity = ClientEntity.FromDomain(client);
        var scopeIds = client.ScopeIds.ToList();
        entity.Scopes = scopeIds.Count > 0 ? await db.Scopes.Where(s => scopeIds.Contains(s.Id)).ToListAsync() : [];

        db.Clients.Add(entity);
        await db.SaveChangesAsync();
        return client.Id;
    }

    public static async Task<Guid> CreateTenantIfNotExistsAsync(this AuthDbContext db, string name, string slug)
    {
        var existingId = await db.Tenants.Where(t => t.Name == name).Select(t => (Guid?)t.Id).SingleOrDefaultAsync();
        if (existingId is not null)
        {
            return existingId.Value;
        }

        var id = Guid.CreateVersion7();
        db.Tenants.Add(new TenantEntity
        {
            Id = id,
            Name = name,
            Slug = slug,
            CreatedAt = DateTimeOffset.UtcNow,
            NormalizedName = Tenant.NormalizeName(name)
        });

        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid?> GetTenantIdByNameAsync(this AuthDbContext db, string name)
    {
        var normalizedName = Tenant.NormalizeName(name);
        return await db.Tenants.Where(t => t.NormalizedName == normalizedName).Select(t => (Guid?)t.Id).SingleOrDefaultAsync();
    }

    public static async Task<Guid> CreateResourceServerIfNotExistsAsync(this AuthDbContext db, string audience, string displayName, bool isAuthServer = false)
    {
        var entity = await db.ResourceServers.SingleOrDefaultAsync(s => s.Audience == audience);
        if (entity is not null)
        {
            return entity.Id;
        }

        var id = Guid.CreateVersion7();
        entity = new ResourceServerEntity
        {
            Id = id,
            Audience = audience,
            DisplayName = displayName,
            IsAuthServer = isAuthServer,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Add(entity);
        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid> CreateRoleIfNotExistsAsync(this AuthDbContext db, Guid resourceServerId, string roleName, string description)
    {
        roleName = roleName.Trim().ToLowerInvariant();
        var existingRoleId = await db.Roles.Where(r => r.Name == roleName).Select(r => (Guid?)r.Id).SingleOrDefaultAsync();
        if (existingRoleId is not null)
        {
            return existingRoleId.Value;
        }

        var id = Guid.CreateVersion7();
        db.Roles.Add(new RoleEntity
        {
            Id = id,
            ResourceServerId = resourceServerId,
            Name = roleName,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid> CreateScopeIfNotExistsAsync(this AuthDbContext db, string name, string displayName, Guid? resourceServerId, bool isStandard)
    {
        var existingId = await db.Scopes
            .Where(s => s.Name == name && s.ResourceServerId == resourceServerId)
            .Select(s => (Guid?)s.Id)
            .SingleOrDefaultAsync();

        if (existingId is not null)
        {
            return existingId.Value;
        }
        Guid id = Guid.CreateVersion7();
        db.Scopes.Add(new ScopeEntity
        {
            Id = id,
            Name = name,
            DisplayName = displayName,
            ResourceServerId = resourceServerId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsStandard = isStandard
        });

        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<List<Guid>> GetScopeIdsByNamesAsync(this AuthDbContext db, IEnumerable<string> names) =>
        await db.Scopes.Where(s => names.Contains(s.Name)).Select(s => s.Id).ToListAsync();

    public static async Task<Guid> CreateUserIfNotExistsAsync(this AuthDbContext db, IPasswordHasher passwordHasher, string email, string password)
    {
        var normalizedEmail = User.NormalizeEmail(email);

        var existingId = await db.Users.Where(u => u.NormalizedEmail == normalizedEmail).Select(u => (Guid?)u.Id).SingleOrDefaultAsync();
        if (existingId is not null)
        {
            return existingId.Value;
        }

        var userId = Guid.CreateVersion7();

        db.Users.Add(new UserEntity
        {
            Id = userId,
            PasswordHash = passwordHasher.Hash(password),
            Email = email,
            NormalizedEmail = normalizedEmail,
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.TakenEmails.Add(new TakenEmailEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            NormalizedEmail = normalizedEmail,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        return userId;
    }

    public static async Task CreateUserRoleIfNotExistAsync(this AuthDbContext db, Guid userId, Guid roleId, Guid? tenantId)
    {
        if (await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantId))
            return;

        var id = Guid.CreateVersion7();
        db.Add(new UserRoleEntity
        {
            Id = id,
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
