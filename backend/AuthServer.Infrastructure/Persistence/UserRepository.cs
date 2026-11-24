using AuthServer.Domain.Common;
using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class UserRepository(AuthDbContext db) : IUserRepository
{
    // Split query: roles and external ids in their own queries, so their rows are not multiplied.
    private IQueryable<UserEntity> ReadQuery =>
        db.Users.AsNoTracking().Include(u => u.UserRoles).Include(u => u.ExternalIds).AsSplitQuery();

    public async Task<User?> GetByExternalIdAsync(string provider, string providerUserId)
    {
        var entity = await ReadQuery.SingleOrDefaultAsync(u =>
            u.ExternalIds.Any(x => x.Provider == provider && x.ProviderUserId == providerUserId));
        return entity?.ToDomain();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var entity = await ReadQuery.SingleOrDefaultAsync(u => u.Id == id);
        return entity?.ToDomain();
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
    {
        var entity = await ReadQuery.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        return entity?.ToDomain();
    }

    public async Task<List<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        var distinctIds = ids.Distinct().ToList();

        var entities = await ReadQuery
            .Where(u => distinctIds.Contains(u.Id))
            .OrderBy(u => u.NormalizedEmail)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<PagedList<User>> GetPagedListAsync(UserPagedListQuery query)
    {
        var users = ReadQuery;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = query.SearchTerm.ToContainsPattern();

            // Matches the email or a role name. Two separate queries joined with Union, so that each
            // can use its own index (ix_users_normalized_email_trgm; ix_roles_name_trgm and
            // ix_user_roles_role_id). One OR across the tables would make PostgreSQL read every user.
            var roleIds = db.Roles.Where(r => EF.Functions.ILike(r.Name, pattern)).Select(r => r.Id);
            var byEmail = db.Users.Where(u => EF.Functions.ILike(u.NormalizedEmail, pattern)).Select(u => u.Id);
            var byRole = db.UserRoles.Where(ur => roleIds.Contains(ur.RoleId)).Select(ur => ur.UserId);
            var matchingIds = byEmail.Union(byRole);

            users = users.Where(u => matchingIds.Contains(u.Id));
        }

        if (query.TenantId is { } tenantId)
        {
            users = users.Where(u => u.UserRoles.Any(ur => ur.TenantId == tenantId));
        }

        return users.OrderBy(u => u.NormalizedEmail).ToPagedListAsync(query, e => e.ToDomain());
    }

    public Task<bool> EmailTakenAsync(string normalizedEmail, Guid? excludeUserId = null) =>
        db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != excludeUserId);

    public async Task<int> DeleteUnusedInvitedAsync(DateTimeOffset createdBefore, DateTimeOffset now)
    {
        var userIds = db.Users
            .Where(u => u.Status == UserStatuses.Invited
                && u.CreatedAt < createdBefore
                && !u.UserRoles.Any()
                && !db.PasswordTokens.Any(t => t.UserId == u.Id && t.UsedAt == null && t.ExpiresAt > now))
            .Select(u => u.Id);

        await using var transaction = await db.Database.BeginTransactionAsync();

        // The rows that point to the users first: all foreign keys are Restrict.
        await db.PasswordTokens.Where(t => userIds.Contains(t.UserId)).ExecuteDeleteAsync();
        await db.RefreshTokens.Where(t => userIds.Contains(t.UserId)).ExecuteDeleteAsync();
        await db.AuthorizationCodes.Where(c => userIds.Contains(c.UserId)).ExecuteDeleteAsync();
        await db.UserExternalIds.Where(x => userIds.Contains(x.UserId)).ExecuteDeleteAsync();
        var deletedCount = await db.Users.Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync();

        await transaction.CommitAsync();
        return deletedCount;
    }

    public async Task CreateAsync(User user)
    {
        var entity = UserEntity.FromDomain(user);
        SyncRoles(entity, user);
        AddNewExternalIds(entity, user);

        db.Users.Add(entity);
        await SaveAsync();
    }

    public async Task UpdateAsync(User user)
    {
        var entity = await db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalIds)
            .AsSplitQuery()
            .SingleAsync(u => u.Id == user.Id);

        entity.UpdateFrom(user);
        SyncRoles(entity, user);
        AddNewExternalIds(entity, user);

        await SaveAsync();
    }

    private void SyncRoles(UserEntity entity, User user)
    {
        // Removed explicitly: all foreign keys use DeleteBehavior.Restrict, so EF does not delete orphans.
        foreach (var removed in entity.UserRoles.Where(ur => !user.HasRole(ur.RoleId, ur.TenantId)).ToList())
        {
            entity.UserRoles.Remove(removed);
            db.UserRoles.Remove(removed);
        }

        // Added explicitly: the key is set here (Guid.CreateVersion7), and EF treats a new entity with
        // a set generated key found only through a navigation as existing, which would turn the
        // insert into an UPDATE of a missing row (DbUpdateConcurrencyException).
        foreach (var role in user.Roles.Where(r => !entity.UserRoles.Any(ur => ur.RoleId == r.RoleId && ur.TenantId == r.TenantId)).ToList())
        {
            var added = new UserRoleEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = entity.Id,
                RoleId = role.RoleId,
                TenantId = role.TenantId,
                CreatedAt = role.CreatedAt
            };

            entity.UserRoles.Add(added);
            db.UserRoles.Add(added);
        }
    }

    // External ids are only added; the domain has no way to remove them. Added explicitly, as in SyncRoles.
    private void AddNewExternalIds(UserEntity entity, User user)
    {
        foreach (var externalId in user.ExternalIds.Where(x =>
            !entity.ExternalIds.Any(e => e.Provider == x.Provider && e.ProviderUserId == x.ProviderUserId)))
        {
            var added = new UserExternalIdEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = entity.Id,
                Provider = externalId.Provider,
                ProviderUserId = externalId.ProviderUserId,
                CreatedAt = externalId.CreatedAt
            };

            entity.ExternalIds.Add(added);
            db.UserExternalIds.Add(added);
        }
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
            ConstraintName: AuthDbContext.UserNormalizedEmailIndex
        })
        {
            // A concurrent request took the email after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(User.EmailTakenError());
        }
    }
}
