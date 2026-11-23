namespace AuthServer.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail);

    // Ordered by normalized email.
    Task<List<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by normalized email. SearchTerm matches the email.
    Task<PagedList<User>> GetPagedListAsync(UserPagedListQuery query);

    // Whether another user has the normalized email.
    Task<bool> EmailTakenAsync(string normalizedEmail, Guid? excludeUserId = null);

    Task CreateAsync(User user);

    Task UpdateAsync(User user);
}
