using AuthServer.Domain.Users;
using AspNetPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<object>;
using AspNetVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace AuthServer.Infrastructure.Users;

// Uses the ASP.NET Core Identity password hash format (PBKDF2). The user argument of the
// Identity hasher is not used by its default implementation.
public class PasswordHasher : IPasswordHasher
{
    private static readonly object NoUser = new();
    private readonly AspNetPasswordHasher _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public PasswordCheck Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(NoUser, passwordHash, password) switch
        {
            AspNetVerificationResult.Success => PasswordCheck.Success,
            AspNetVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            _ => PasswordCheck.Failed
        };
}
