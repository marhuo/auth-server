namespace AuthServer.Web.Application.Clients;

public enum ClientTypes
{
    PublicApplication,
    ConfidentialApplication,
    ConfidentialMachine
}
public static class ClientTypesExtensions
{
    extension(ClientTypes type)
    {
        public bool IsConfidential =>
            type is ClientTypes.ConfidentialApplication or ClientTypes.ConfidentialMachine;

        public bool SignsInUsers =>
            type is ClientTypes.PublicApplication or ClientTypes.ConfidentialApplication;
    }
}