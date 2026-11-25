namespace AuthServer.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail);

    // Ordered by normalized email.
    Task<List<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by normalized email. SearchTerm matches the email.
    Task<PagedList<User>> GetPagedListAsync(UserPagedListQuery query);

    Task<User?> GetByExternalIdAsync(string provider, string providerUserId);

    // Whether another user has the normalized email.
    Task<bool> EmailTakenAsync(string normalizedEmail, Guid? excludeUserId = null);

    // Physically deletes users who are still Invited, have no roles and were created before the given
    // time. Returns the number of deleted users.
    Task<int> DeleteUnusedInvitedAsync(DateTimeOffset createdBefore);

    Task CreateAsync(User user);

    Task UpdateAsync(User user);
}
