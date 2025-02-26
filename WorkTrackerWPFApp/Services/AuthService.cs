using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using WorkTrackerDesktopWPFApp.Responses;
using WorkTrackerWPFApp.Responses;

namespace WorkTrackerDesktopWPFApp.Services
{

    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _authUrl;

        public AuthService(HttpClient httpClient, IConfiguration config)
        {
            // Set up Serilog to log to both console and file
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YourApp", "logs", "log.txt");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()  // Output logs to console
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day) // Log to file
                .CreateLogger();

            _httpClient = httpClient;
            _authUrl = config["ApiBaseUrl"] ?? "https://localhost:7119/api/";

        }


        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync(_authUrl + "Accounts/Login", loginData);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the raw JSON string into a strongly-typed LoginResponse object using JsonConvert
                    var loginResponseDto = JsonConvert.DeserializeObject<LoginResponse>(content);
                    return new LoginResponse
                    {
                        Success = loginResponseDto.Success,
                        Message = loginResponseDto.Message,
                        Token = loginResponseDto.Token, // Extract token directly
                        Username = loginResponseDto.Username, // Extract username
                        UserId = loginResponseDto.UserId, // Extract userId
                        Roles = loginResponseDto.Roles, // Extract roles
                        LocalSessionExpireDate = DateTime.Now.AddHours(3)// Set session expiration
                    };
                }
                else
                {
                    // Return a failed response if login fails
                    return new LoginResponse
                    {
                        Success = false,
                        Message = content,
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the error using the injected logger
                Log.Error(ex, "Error in Login with email:", email);

                // Return a failed response on error
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Error in Login: \n {ex.Message}"
                };
            }

        }
        public async Task<ValidationResponse> ResetPasswordAsync(string email)
        {
            try
            {
                Log.Information("Resetting password for email: {Email}", email);
                var forgotPasswordData = new { Email = email };
                var response = await _httpClient.PostAsJsonAsync(_authUrl + "Accounts/RequestPasswordReset", forgotPasswordData);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var forgotPasswordResponseDto = JsonConvert.DeserializeObject<ValidationResponse>(content);
                    return new LoginResponse
                    {
                        Success = forgotPasswordResponseDto.Success,
                        Message = forgotPasswordResponseDto.Message,
                    };

                }
                else
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = content,
                    };

                }
            }
            catch (Exception ex)
            {
                // Log the error using the injected logger
                Log.Error(ex, "Error resetting password for email: {Email}", email);
                return new ValidationResponse
                {
                    Success = false,
                    Message = $" Error resetting password \n {ex.Message}"
                };
            }
        }
    }
}
