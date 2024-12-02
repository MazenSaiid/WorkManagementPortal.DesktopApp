using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace WorkTrackerDesktop.Services
{
    public class WorkTimerService
    {
    
        private readonly HttpClient _httpClient;
        private const string WorkSessionUrl = "https://localhost:7119/api/WorkTrackings/"; // Replace with your API URL

        public WorkTimerService()
        {
            
            _httpClient = new HttpClient();
            
        }

        public async Task<HttpResponseMessage> StartAsync()
        {
            try
            {
                var data = new
                {
                 
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PostAsync(WorkSessionUrl+ "ClockIn", content);
                return response;
            }
            catch (Exception ex)
            {
                // Handle errors here, such as logging the error
                Console.WriteLine($"Error updating work session: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error in Start Session: {ex.Message}")
                };
            }
        }

        public async Task<HttpResponseMessage> PauseAsync()
        {

            try
            {
                var data = new
                {
                   
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PostAsync(WorkSessionUrl+ "StartPause", content);
                return response;
            }
            catch (Exception ex)
            {
                // Handle errors here, such as logging the error
                Console.WriteLine($"Error updating work session: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error in Pause: {ex.Message}")
                };
            }
        }

        public async Task<HttpResponseMessage> ResumeAsync()
        {

            try
            {
                var data = new
                {
                    
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PostAsync(WorkSessionUrl+ "EndPause", content);
                return response;
            }
            catch (Exception ex)
            {
                // Handle errors here, such as logging the error
                Console.WriteLine($"Error updating work session: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error in Resume: {ex.Message}")
                };
            }
        }

        public async Task<HttpResponseMessage> StopAsync()
        {

            try
            {
                var data = new
                {
                    
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PostAsync(WorkSessionUrl+ "ClockOut", content);
                return response;
            }
            catch (Exception ex)
            {
                // Handle errors here, such as logging the error
                Console.WriteLine($"Error updating work session: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error in Stop: {ex.Message}")
                };
            }
        }

        

    }
}
