namespace AuthServer.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail);

    // Ordered by normalized email.
    Task<List<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    // Ordered by normalized email. SearchTerm matches the email.
    Task<PagedList<User>> GetPagedListAsync(PagedListQuery query);

    // Whether the normalized email is or has been used by another user.
    // The user given in excludeUserId may take back an email they have had before.
    Task<bool> EmailTakenAsync(string normalizedEmail, Guid? excludeUserId = null);

    // Also reserves the email (taken_emails).
    Task CreateAsync(User user);

    // Also reserves a changed email (taken_emails).
    Task UpdateAsync(User user);
}
