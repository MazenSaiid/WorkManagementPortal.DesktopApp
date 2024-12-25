using Serilog;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.IO;
using System.Net.NetworkInformation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Image = SixLabors.ImageSharp.Image;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace WorkTrackerDesktopWPFApp.Services
{
    public class ScreenshotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _uploadUrl;
        private readonly int _syncInterval; // in milliseconds
        private System.Timers.Timer _syncTimer;
        private readonly string _username;

        // Paths
        private readonly string _baseDirectory;
        private readonly string _logFilePath;
        private readonly string _pendingScreenshotFilePath;

        public ScreenshotService(IConfiguration config)
        {
            _username = Environment.UserName;

            // Initialize paths
            _baseDirectory = Path.Combine("C:\\", "Users", _username, "WorkTrackerScreenshots");
            _logFilePath = Path.Combine(_baseDirectory, "logs", "log.txt");
            _pendingScreenshotFilePath = Path.Combine(_baseDirectory, "pending_screenshots.txt");

            // Create necessary directories
            CreateDirectoryIfNotExists(_baseDirectory);
            CreateDirectoryIfNotExists(Path.GetDirectoryName(_logFilePath));

            // Set up logging
            SetUpLogging();

            // Read configuration settings
            _uploadUrl = config["ApiBaseUrl"] ?? "https://localhost:7119/api/";
            _syncInterval = int.TryParse(config["SyncInterval"], out var interval) ? interval : 60000; // Default 60 seconds

            _httpClient = new HttpClient();
            _syncTimer = new System.Timers.Timer(_syncInterval); // Sync every interval defined in settings
            _syncTimer.Elapsed += async (sender, e) => await CaptureScreenshotAsync(); // Capture and sync periodically
        }

        private void SetUpLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()  // Output logs to console
                .WriteTo.File(_logFilePath, rollingInterval: RollingInterval.Day) // Log to file
                .CreateLogger();
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void StartPeriodicSync()
        {
            _syncTimer.Start();
        }

        public async Task CaptureScreenshotAsync()
        {
            try
            {
                // Capture the screenshot on the UI thread
                Stream screenshotResult = await CaptureFullScreenAsync(); // Ensure this returns Stream

                if (screenshotResult == null)
                {
                    Log.Error("Screenshot capture failed or is not supported on this platform.");
                    return;
                }

                // Generate a unique filename using a timestamp or GUID
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string screenshotPath = Path.Combine(_baseDirectory, $"{timestamp}_screenshot.png");

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

        private async Task<Stream> CaptureFullScreenAsync()
        {
            try
            {
                // Capture the whole screen
                System.Drawing.Rectangle screenArea = Screen.PrimaryScreen.Bounds;
                Bitmap screenshot = new Bitmap(screenArea.Width, screenArea.Height);

                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    g.CopyFromScreen(screenArea.Location, System.Drawing.Point.Empty, screenArea.Size);
                }

                // Save the screenshot to a memory stream
                var memoryStream = new MemoryStream();
                screenshot.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                // Reset the position of the memory stream and return
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                // Log error if necessary
                Console.WriteLine("Error capturing full-screen: " + ex.Message);
                return null;
            }
        }


        private async Task UploadScreenshotWithRetryAsync(string filePath)
        {
            try
            {
                var content = new MultipartFormDataContent();

                // Use a 'using' statement to ensure the file stream is properly closed after the operation
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.Add("Content-Type", "image/png"); // Add appropriate file type, if necessary
                    content.Add(fileContent, "File", Path.GetFileName(filePath)); // The key should be "File" matching the backend

                    var userId = WorkSessionService.Instance.UserId = UserSessionService.Instance.UserId;
                    content.Add(new StringContent(userId), "UserId"); // The key should be "UserId"

                    var workLogId = WorkSessionService.Instance.WorkLogId;
                    if (workLogId.HasValue)  // Check if WorkLogId is not null
                    {
                        content.Add(new StringContent(workLogId.Value.ToString()), "WorkLogId"); // The key should be "WorkLogId"
                    }

                        var response = await _httpClient.PostAsync(_uploadUrl + "ScreenShotTrackings/UploadScreenShot", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Screenshot uploaded successfully.");

                        // Explicitly dispose of the content to release file lock
                        fileContent.Dispose();
                        bool deleted = false;
                        int retryCount = 3;
                        while (retryCount > 0 && !deleted)
                        {
                            try
                            {
                                File.Delete(filePath); // Try deleting the file
                                Log.Information("File deleted successfully.");
                                deleted = true; // If successful, break the loop

                            }
                            catch (IOException)
                            {
                                retryCount--;
                                Log.Warning("File is still in use. Retrying...");
                                await Task.Delay(500); // Wait a bit before retrying
                            }
                        }

                        if (!deleted)
                        {
                            Log.Warning("Failed to delete screenshot after multiple attempts.");
                        }

                    }
                    else
                    {
                        Log.Warning("Failed to upload screenshot. Server returned: {StatusCode}", response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Attempt failed to upload screenshot");
            }
        }


        private async Task SaveScreenshotToFileAsync(Stream screenshotStream, string filePath)
        {
            try
            {
                if (screenshotStream != null)
                {
                    // Load the image using SixLabors.ImageSharp (or another image library you're using)
                    using (var image = await Image.LoadAsync(screenshotStream))
                    {
                        // Save the image to the file path in PNG format
                        await image.SaveAsync(filePath, new PngEncoder());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving screenshot");
            }
        }

        private async Task SaveScreenshotLocallyAsync(string filePath)
        {
            await File.AppendAllTextAsync(_pendingScreenshotFilePath, filePath + Environment.NewLine);
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
            if (!File.Exists(_pendingScreenshotFilePath))
            {
                return new List<string>(); // No pending screenshots
            }

            return File.ReadAllLines(_pendingScreenshotFilePath);
        }
    }
}
