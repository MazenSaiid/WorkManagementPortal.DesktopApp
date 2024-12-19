namespace WorkTrackerDesktopWPFApp.Responses
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public string UserId { get; set; }
        public List<string> Roles { get; set; }
        public DateTime LocalSessionExpireDate { get; set; }
    }
}
