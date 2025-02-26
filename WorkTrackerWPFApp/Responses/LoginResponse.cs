using WorkTrackerWPFApp.Responses;

namespace WorkTrackerDesktopWPFApp.Responses
{
    public class LoginResponse : ValidationResponse
    {
        public string Username { get; set; }
        public string UserId { get; set; }
        public List<string> Roles { get; set; }
        public DateTime LocalSessionExpireDate { get; set; }
    }
}
