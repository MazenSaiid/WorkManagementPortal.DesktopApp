using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace WorkTrackerDesktop.Services
{
    public class ScreenshotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _uploadUrl;
        private readonly int _syncInterval; // in milliseconds
        private System.Timers.Timer _syncTimer;


        public ScreenshotService(IConfiguration config)
        {
            // Set up Serilog to log to both console and file C:\\Users\\abdalla\\AppData\\Roaming\\YourApp\\logs\\log.txt
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YourApp", "logs", "log.txt");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()  // Output logs to console
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day) // Log to file
                .CreateLogger();

            // Read configuration settings
            _uploadUrl = config["ApiBaseUrl"] ?? "https://localhost:7119/api/";
            _syncInterval = int.TryParse(config["SyncInterval"], out var interval) ? interval : 60000; // Default 60 seconds

            _httpClient = new HttpClient();
            _syncTimer = new System.Timers.Timer(_syncInterval); // Sync every interval defined in settings
            _syncTimer.Elapsed += async (sender, e) => await CaptureScreenshotAsync(); // Capture and sync periodically
        }


        public void StartPeriodicSync()
        {
            _syncTimer.Start();
        }

        public async Task CaptureScreenshotAsync()
        {
            try
            {
                // Capture the screenshot
                var screenshotResult = await Screenshot.CaptureAsync();
                var screenshotPath = Path.Combine(FileSystem.CacheDirectory, "screenshot.png");

                // Save locally if no internet connection
                await SaveScreenshotToFileAsync(screenshotResult, screenshotPath);

                if (IsInternetAvailable())
                {
                    // If internet is available, upload to the backend
                    await UploadScreenshotWithRetryAsync(screenshotPath);
                }
                else
                {
                    // Save screenshot path locally for later upload
                    await SaveScreenshotLocallyAsync(screenshotPath);
                }

                // Sync any pending screenshots (those saved when offline)
                await SyncPendingScreenshots();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error capturing screenshot");
            }
        }

        private bool IsInternetAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        private async Task SyncPendingScreenshots()
        {
            try
            {
                var screenshotPaths = GetPendingScreenshots();
                foreach (var screenshotPath in screenshotPaths)
                {
                    if (IsInternetAvailable())
                    {
                        await UploadScreenshotWithRetryAsync(screenshotPath);
                        DeleteLocalScreenshot(screenshotPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error syncing pending screenshots");
            }
        }

        private void DeleteLocalScreenshot(string screenshotPath)
        {
            try
            {
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath); // Delete the screenshot file after successful upload
                    Log.Information("Local screenshot file deleted: {ScreenshotPath}", screenshotPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting local screenshot");
            }
        }

        private IEnumerable<string> GetPendingScreenshots()
        {
            string pendingFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pending_screenshots.txt");
            if (!File.Exists(pendingFilePath))
            {
                return new List<string>(); // No pending screenshots
            }

            return File.ReadAllLines(pendingFilePath);
        }

        private async Task SaveScreenshotToFileAsync(IScreenshotResult screenshotResult, string filePath)
        {
            try
            {
                var screenshotStream = await screenshotResult.OpenReadAsync();
                if (screenshotStream != null)
                {
                    using (var image = await Image.LoadAsync(screenshotStream))
                    {
                        await image.SaveAsync(filePath, new PngEncoder());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving screenshot");
            }
        }

        private async Task UploadScreenshotWithRetryAsync(string filePath)
        {
            int retries = 5;
            int delay = 1000; // 1 second delay
            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    var content = new MultipartFormDataContent();
                    var fileContent = new StreamContent(new FileStream(filePath, FileMode.Open));
                    fileContent.Headers.Add("Content-Type", "image/png");
                    content.Add(fileContent, "file", "screenshot.png");

                    var response = await _httpClient.PostAsync(_uploadUrl + "ScreenShotTrackings/UploadScreenShot", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Screenshot uploaded successfully.");
                        File.Delete(filePath); // Delete the local file after successful upload
                        return; // Success, break out of the loop
                    }
                    else
                    {
                        Log.Warning("Failed to upload screenshot. Server returned: {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Attempt failed to upload screenshot", attempt);
                }

                // Exponential backoff for retries
                await Task.Delay(delay);
                delay *= 2; // Increase the delay with each attempt
            }

            Log.Error("Failed to upload screenshot after {Retries} attempts", retries);
        }

        private async Task SaveScreenshotLocallyAsync(string filePath)
        {
            var localStoragePath = Path.Combine(FileSystem.AppDataDirectory, "pending_screenshots.txt");
            await File.AppendAllTextAsync(localStoragePath, filePath + Environment.NewLine);
        }
    }
}
