
using Newtonsoft.Json;
using System.Net;
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
            try
            {
                var loginData = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"https://localhost:7119/api/Accounts/Login", loginData);
                return response;
            }
            catch (Exception ex)
            {

                // Handle errors here, such as logging the error
                Console.WriteLine($"Error in Login: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error in Login: {ex.Message}")
                };


            }
            
        }
        public async Task<HttpResponseMessage> LogoutAsync()
        {
            try
            {
                var loginData = "0";
                var response = await _httpClient.PostAsJsonAsync($"https://localhost:7119/api/Accounts/Logout", loginData);
                return response;
            }
            catch (Exception ex)
            {
                // Handle errors here, such as logging the error
                Console.WriteLine($"Error in Logout: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error in Logout: {ex.Message}")
                };

            }
            
        }
        public async Task<HttpResponseMessage> ResetPasswordAsync(string email)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { email }), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://localhost:7119/api/Accounts/ResetPassword", content);
                return response;
            }
            catch (Exception ex)
            {

                // Handle errors here, such as logging the error
                Console.WriteLine($"Error reseting password: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error resetting password: {ex.Message}")
                };
            }
            
        }
    }
}
