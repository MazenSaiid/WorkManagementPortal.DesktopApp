using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace WorkTrackerDesktop.Services
{
    public class OfflineSyncService
    {
        private readonly HttpClient _httpClient;
        private const string WorkSessionUrl = "https://your-api-endpoint.com/work-session";
        private const string ScreenshotUrl = "https://your-api-endpoint.com/upload-screenshot"; // For screenshots
        private System.Timers.Timer _syncTimer;

        public OfflineSyncService()
        {
            _httpClient = new HttpClient();
            _syncTimer = new System.Timers.Timer(300000); // Sync every 5 minutes
            _syncTimer.Elapsed += (sender, e) => SyncDataAsync();
        }

        public void StartPeriodicSync()
        {
            _syncTimer.Start();
        }

        private async Task SyncDataAsync()
        {
            try
            {
                // Sync work session data
                var workSessions = GetPendingWorkSessions(); // This method reads data stored locally
                foreach (var session in workSessions)
                {
                    await UploadWorkSessionData(session);
                }

                // Sync screenshots
                var screenshotPaths = GetPendingScreenshots(); // This method reads paths of pending screenshots
                foreach (var screenshotPath in screenshotPaths)
                {
                    await UploadScreenshotData(screenshotPath);
                }
            }
            catch (Exception ex)
            {
                // Log errors, or show messages as needed
                Console.WriteLine($"Error during periodic sync: {ex.Message}");
            }
        }

        private async Task UploadWorkSessionData(string workSession)
        {
            try
            {
                var content = new StringContent(workSession); // You would serialize your work session data here
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PostAsync(WorkSessionUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Work session data uploaded successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to upload work session data.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading work session data: {ex.Message}");
            }
        }

        private async Task UploadScreenshotData(string screenshotPath)
        {
            try
            {
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(new FileStream(screenshotPath, FileMode.Open));
                fileContent.Headers.Add("Content-Type", "image/png");
                content.Add(fileContent, "file", "screenshot.png");

                var response = await _httpClient.PostAsync(ScreenshotUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Screenshot uploaded successfully.");
                    File.Delete(screenshotPath); // Delete the local screenshot after successful upload
                }
                else
                {
                    Console.WriteLine("Failed to upload screenshot.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading screenshot: {ex.Message}");
            }
        }

        // Methods to fetch locally saved sessions and screenshots:
        private IEnumerable<string> GetPendingWorkSessions()
        {
            // Load work session data from a local file or database
            return File.ReadAllLines(Path.Combine(FileSystem.AppDataDirectory, "pending_sessions.txt"));
        }

        private IEnumerable<string> GetPendingScreenshots()
        {
            // Load screenshot paths from a local file
            return File.ReadAllLines(Path.Combine(FileSystem.AppDataDirectory, "pending_screenshots.txt"));
        }
    }
}
