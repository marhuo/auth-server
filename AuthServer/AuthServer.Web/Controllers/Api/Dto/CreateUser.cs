namespace AuthServer.Web.Controllers.Api.Dto
{
    public class CreateUser
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string[] Roles { get; set; } = [];
    }
}
