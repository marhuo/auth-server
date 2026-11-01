namespace AuthServer.Web.Pages.Admin
{
    public class UserVM
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public int FailedLogins { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
    }
}
