using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace WorkTrackerDesktop.Services
{
    public class WorkTimerService
    {
        private System.Timers.Timer _timer;
        private TimeSpan _elapsedTime;
        private bool _isPaused;
        private readonly HttpClient _httpClient;
        private const string WorkSessionUrl = "https://your-api-endpoint.com/work-session"; // Replace with your API URL

        public WorkTimerService()
        {
            _elapsedTime = TimeSpan.Zero;
            _isPaused = false;
            _httpClient = new HttpClient();
            _timer = new System.Timers.Timer(); 
        }

        public void Start()
        {
            _isPaused = false;
            _timer.Start();
            // Start work session on the backend
            SendWorkSessionUpdateAsync("start");
        }

        public void Pause()
        {
            _isPaused = true;
            _timer.Stop();
            // Pause work session on the backend
            SendWorkSessionUpdateAsync("pause");
        }

        public void Resume()
        {
            _isPaused = false;
            _timer.Start();
            // Resume work session on the backend
            SendWorkSessionUpdateAsync("resume");
        }

        public void Stop()
        {
            _timer.Stop();
            // Send stop session request to the backend
            SendWorkSessionUpdateAsync("stop");
            // Final save work session data or perform clean up
        }

        private async Task SendWorkSessionUpdateAsync(string action)
        {
            try
            {
                var data = new
                {
                    Action = action,
                    ElapsedTime = _elapsedTime.ToString(),
                    Timestamp = DateTime.Now
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PostAsync(WorkSessionUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Work session {action} successful.");
                }
                else
                {
                    Console.WriteLine($"Failed to update work session {action}.");
                }
            }
            catch (Exception ex)
            {
                // Handle errors here, such as logging the error
                Console.WriteLine($"Error updating work session: {ex.Message}");
            }
        }

    }
}
