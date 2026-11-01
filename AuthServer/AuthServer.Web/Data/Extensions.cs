using AuthServer.Web.Application.Clients;
using AuthServer.Web.Application.Tenants;
using AuthServer.Web.Application.Users;
using AuthServer.Web.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Web.Data;

public static class Extensions
{
    public static async Task<Guid> CreatePublicApplicationClientIfNotExistsAsync(this AuthDbContext db, string clientId, string displayName, Guid? tenantId, Guid resourceServerId, IEnumerable<string> redirectUris, IEnumerable<string> postLogoutRedirectUris, IEnumerable<Guid> allowedScopeIds)
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
            ResourceServerId = resourceServerId,
            Scopes = scopeEntities
        };

        db.Clients.Add(clientEntity);
        await db.SaveChangesAsync();
        return id;
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
            NormalizedName = TenantService.NormalizeName(name)
        });

        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid?> GetTenantIdByNameAsync(this AuthDbContext db, string name)
    {
        var normalizedName = TenantService.NormalizeName(name);
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

    public static async Task<Guid> CreateUserIfNotExistsAsync(this AuthDbContext db, IPasswordHasher<UserEntity> passwordHasher, string userName, string email, string password)
    {
        var normalizedUsername = UserService.NormalizeUsername(userName);

        var existingId = await db.Users.Where(u => u.NormalizedUserName == normalizedUsername).Select(u => (Guid?)u.Id).SingleOrDefaultAsync();
        if (existingId is not null)
        {
            return existingId.Value;
        }

        var userId = Guid.CreateVersion7();


        db.Users.Add(new UserEntity
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = normalizedUsername,
            PasswordHash = passwordHasher.HashPassword(null!, password),
            Email = email,
            NormalizedEmail = UserService.NormalizeEmail(email),
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.TakenUsernames.Add(new TakenUsernameEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            NormalizedUsername = normalizedUsername,
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
