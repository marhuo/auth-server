using AuthServer.Domain.Common;
using AuthServer.Domain.Roles;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class RoleRepository(AuthDbContext db) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(Guid id)
    {
        var entity = await db.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id);
        return entity?.ToDomain();
    }

    public async Task<List<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        var entities = await db.Roles
            .AsNoTracking()
            .Where(r => distinctIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<PagedList<Role>> GetPagedListAsync(RolePagedListQuery query)
    {
        var roles = db.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = query.SearchTerm.ToContainsPattern();
            roles = roles.Where(r =>
                EF.Functions.ILike(r.Name, pattern) ||
                EF.Functions.ILike(r.Description, pattern));
        }

        if (query.ResourceServerId is { } resourceServerId)
            roles = roles.Where(r => r.ResourceServerId == resourceServerId);

        return roles.OrderBy(r => r.Name).ToPagedListAsync(query, e => e.ToDomain());
    }

    public async Task<Dictionary<string, Guid>> GetProductRoleIdsByNamesAsync(IReadOnlyCollection<string> names)
    {
        if (names.Count == 0)
            return [];

        return await db.Roles
            .Where(r => names.Contains(r.Name) && !r.ResourceServer.IsAuthServer)
            .ToDictionaryAsync(r => r.Name, r => r.Id);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null) =>
        db.Roles.AnyAsync(r => r.Name == name && r.Id != excludeId);

    public Task<bool> IsAssignedToUsersAsync(Guid roleId) =>
        db.UserRoles.AnyAsync(ur => ur.RoleId == roleId);

    public Task<bool> AnyForResourceServerAsync(Guid resourceServerId) =>
        db.Roles.AnyAsync(r => r.ResourceServerId == resourceServerId);

    public Task<bool> AnyRoleInTenantAsync(Guid tenantId) =>
        db.UserRoles.AnyAsync(ur => ur.TenantId == tenantId);

    public Task<List<string>> GetAuthServerRoleNamesAsync(Guid userId) =>
        db.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role.ResourceServer.IsAuthServer)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

    public Task<List<string>> GetTenantRoleNamesAsync(Guid userId, Guid tenantId) =>
        db.UserRoles
            .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

    public Task<List<Guid>> GetAuthServerRoleIdsAsync(Guid userId) =>
        db.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role.ResourceServer.IsAuthServer)
            .Select(ur => ur.RoleId)
            .ToListAsync();

    public Task<bool> HasOtherActiveHolderAsync(Guid roleId, Guid userId, DateTimeOffset now) =>
        db.UserRoles.AnyAsync(ur =>
            ur.RoleId == roleId
            && ur.UserId != userId
            && !ur.User.Locked
            && (ur.User.LockedUntil == null || ur.User.LockedUntil <= now));

    public async Task CreateAsync(Role role)
    {
        db.Roles.Add(RoleEntity.FromDomain(role));
        await SaveAsync();
    }

    public async Task UpdateAsync(Role role)
    {
        var entity = await db.Roles.SingleAsync(r => r.Id == role.Id);
        entity.UpdateFrom(role);
        await SaveAsync();
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
            ConstraintName: AuthDbContext.RoleNameIndex
        })
        {
            // A concurrent request took the name after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(Role.NameTakenError());
        }
    }
}
