using AuthServer.Web.Application.Clients;

namespace AuthServer.Web.Dto.Admin.Clients;

public enum ClientDtoTypes
{
    PublicApplication,
    ConfidentialApplication,
    ConfidentialMachine
}
public static class ClientDtoTypesExtensions
{
    extension(ClientDtoTypes dto)
    {
        public ClientTypes ToDomain() => dto switch
        {
            ClientDtoTypes.ConfidentialApplication => ClientTypes.ConfidentialApplication,
            ClientDtoTypes.ConfidentialMachine => ClientTypes.ConfidentialMachine,
            ClientDtoTypes.PublicApplication => ClientTypes.PublicApplication,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };
    }
    extension(ClientTypes domainType)
    {
        public ClientDtoTypes ToDto() => domainType switch
        {
            ClientTypes.ConfidentialApplication => ClientDtoTypes.ConfidentialApplication,
            ClientTypes.ConfidentialMachine => ClientDtoTypes.ConfidentialMachine,
            ClientTypes.PublicApplication => ClientDtoTypes.PublicApplication,
            _ => throw new ArgumentOutOfRangeException(nameof(domainType), domainType, null)
        };
    }
}