namespace AuthServer.Application.Clients.Messages;

public class ClientChangeSecretRequest
{
    public required Guid Id { get; init; }
    public required string NewSecret { get; init; }
}
