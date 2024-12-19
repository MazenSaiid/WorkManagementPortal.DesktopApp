using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerDesktopWPFApp.Services
{
    public class UserSessionService
    {
        private static UserSessionService _instance;

        public static UserSessionService Instance => _instance ??= new UserSessionService();

        public string Username { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public List<string> Roles { get; set; }
        public DateTime? SessionExpiration { get; set; }
        public string FirstRole => Roles?.FirstOrDefault();

        // Method to check if the session has expired
        public bool IsSessionExpired()
        {
            if (SessionExpiration.HasValue)
            {
                return DateTime.Now > SessionExpiration.Value;
            }
            return true; // If no expiration time set, assume expired
        }

        // Method to clear the session
        public void ClearSession()
        {
            Username = null;
            UserId = null;
            SessionExpiration = null;
            Token = null;
            Roles = null;
        }
    }

}
