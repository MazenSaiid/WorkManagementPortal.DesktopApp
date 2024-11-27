
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerDesktop.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> LoginAsync(string username, string password)
        {
            var loginData = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync($"https://yourapi.com/api/Accounts/Login", loginData);
            return response;
        }
        public async Task<HttpResponseMessage> LogoutAsync()
        {
            var loginData = "0";
            var response = await _httpClient.PostAsJsonAsync($"https://yourapi.com/api/Accounts/Logout", loginData);
            return response;
        }
        public async Task<HttpResponseMessage> ResetPasswordAsync(string email)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new { email }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://yourapi.com/Accounts/ResetPassword", content);
            return response;
        }
    }
}
