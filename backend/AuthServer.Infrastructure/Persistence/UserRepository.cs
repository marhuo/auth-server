using AuthServer.Domain.Common;
using AuthServer.Domain.Users;
using AuthServer.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthServer.Infrastructure.Persistence;

public class UserRepository(AuthDbContext db) : IUserRepository
{
    private IQueryable<UserEntity> ReadQuery => db.Users.AsNoTracking().Include(u => u.UserRoles);

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

    public Task<PagedList<User>> GetPagedListAsync(PagedListQuery query)
    {
        var users = ReadQuery;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var pattern = query.SearchTerm.ToContainsPattern();
            users = users.Where(u => EF.Functions.ILike(u.NormalizedEmail, pattern));
        }

        return users.OrderBy(u => u.NormalizedEmail).ToPagedListAsync(query, e => e.ToDomain());
    }

    // taken_emails keeps every email a user has had, also after the user changes it.
    public Task<bool> EmailTakenAsync(string normalizedEmail, Guid? excludeUserId = null) =>
        db.TakenEmails.AnyAsync(x => x.NormalizedEmail == normalizedEmail && x.UserId != excludeUserId);

    public async Task CreateAsync(User user)
    {
        var entity = UserEntity.FromDomain(user);
        SyncRoles(entity, user);
        await ReserveEmailAsync(entity);

        db.Users.Add(entity);
        await SaveAsync();
    }

    public async Task UpdateAsync(User user)
    {
        var entity = await db.Users.Include(u => u.UserRoles).SingleAsync(u => u.Id == user.Id);
        var emailChanged = entity.NormalizedEmail != user.NormalizedEmail;

        entity.UpdateFrom(user);
        SyncRoles(entity, user);

        if (emailChanged)
            await ReserveEmailAsync(entity);

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

    // Every normalized email a user has had stays reserved for that user. If the user takes back an
    // email they have had before, the reservation already exists. If it belongs to another user, the
    // unique index rejects the save (the validator checks this first).
    private async Task ReserveEmailAsync(UserEntity entity)
    {
        if (await db.TakenEmails.AnyAsync(x => x.NormalizedEmail == entity.NormalizedEmail && x.UserId == entity.Id))
            return;

        db.TakenEmails.Add(new TakenEmailEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = entity.Id,
            NormalizedEmail = entity.NormalizedEmail,
            CreatedAt = DateTimeOffset.UtcNow
        });
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
            ConstraintName: AuthDbContext.TakenEmailIndex or AuthDbContext.UserNormalizedEmailIndex
        })
        {
            // A concurrent request took the email after the validator checked it.
            db.ChangeTracker.Clear();
            throw new ValidationException(User.EmailTakenError());
        }
    }
}
