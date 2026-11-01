namespace AuthServer.Web.Persistence;
public class SigningKeyEntity
{
    public Guid Id { get; set; }
    public string KeyId { get; set; } = "";
    public string Algorithm { get; set; } = "RS256";
    public string PublicKeyPem { get; set; } = "";
    public string PrivateKeyPem { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
}