using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Domain.Common;

// Random tokens that are given to a client or user but stored only as a hash
// (authorization codes, refresh tokens, invite tokens).
public static class OpaqueToken
{
    // 256 bits of randomness, base64url without padding.
    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    // SHA-256 as hex. The stored form of a token.
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
