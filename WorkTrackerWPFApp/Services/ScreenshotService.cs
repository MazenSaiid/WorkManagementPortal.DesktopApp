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
using WorkTrackerWPFApp.Services;
using System.ComponentModel;
using WorkTrackerWPFApp.Services.Static;

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
        private readonly string _logFilePath;
        private readonly string _pendingScreenshotFilePath;

        // Idle tracking
        private DateTime _lastInputTime;
        private bool _isIdle;
        private int _idleTimeCheck;
        private readonly MouseKeyboardTracker _mouseKeyboardTracker;

        public ScreenshotService(IConfiguration config)
        {
            _logFilePath = PathHelperService.GetBaseDirectoryLogFilePath(config);
            _pendingScreenshotFilePath = Path.Combine(_logFilePath, "pending_screenshots.txt");


            // Read configuration settings
            _uploadUrl = config["ApiBaseUrl"];
            _syncInterval = int.TryParse(config["SyncInterval"], out var interval) ? interval : 60000; // Default 60 seconds
            _mouseKeyboardTracker = new MouseKeyboardTracker(config);
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
                // Check idle status before capturing the screenshot
                bool isIdle = _mouseKeyboardTracker.IsIdle();
                string serializedObject = _mouseKeyboardTracker.GetUserInputData();
                // Capture the screenshot on the UI thread
                Stream screenshotResult = await CaptureFullScreenAsync(); // Ensure this returns Stream

                if (screenshotResult == null)
                {
                    Log.Error("Screenshot capture failed or is not supported on this platform.");
                    return;
                }

                // Generate a unique filename using a timestamp or GUID
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string screenshotPath = Path.Combine(_logFilePath, $"{timestamp}_screenshot.png");

                // Save locally if no internet connection
                await SaveScreenshotToFileAsync(screenshotResult, screenshotPath);

                if (IsInternetAvailable())
                {
                    await UploadScreenshotWithRetryAsync(screenshotPath, isIdle,serializedObject);
                }
                else
                {
                    await SaveScreenshotLocallyAsync(screenshotPath, isIdle, serializedObject);
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
                Log.Information("Screenshot captured successfully.");
                // Reset the position of the memory stream and return
                memoryStream.Position = 0;
                return memoryStream;
                
            }
            catch (Exception ex)
            {
                // Log error if necessary
                Log.Error("Error capturing full-screen: " + ex.Message);
                return null;
            }
        }


        private async Task UploadScreenshotWithRetryAsync(string filePath, bool isIdle,string serializedObject)
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

                    content.Add(new StringContent(isIdle.ToString()), "IsIdle");
                    content.Add(new StringContent(serializedObject), "SerializedTrackingObject");
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
                        Log.Warning($"Failed to upload screenshot. Server returned: {response.StatusCode}" );
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

        private async Task SaveScreenshotLocallyAsync(string filePath, bool isIdle, string serializedObject)
        {
            await File.AppendAllTextAsync(_pendingScreenshotFilePath, $"{filePath}|{isIdle}|{serializedObject} {Environment.NewLine}");
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
                foreach (var entry in screenshotPaths)
                {
                    var parts = entry.Split('|');
                    if (parts.Length == 3)
                    {
                        string filePath = parts[0];
                        bool isIdle = bool.Parse(parts[1]);
                        string serializedObject = parts[2];

                        if (IsInternetAvailable())
                        {
                            await UploadScreenshotWithRetryAsync(filePath, isIdle,serializedObject);
                        }
                    }
                }
                ClearPendingScreenshotFile();
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
                    Log.Information($"Local screenshot file deleted: {screenshotPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting local screenshot");
            }
        }
        private void ClearPendingScreenshotFile()
        {
            try
            {
                File.WriteAllText(_pendingScreenshotFilePath, string.Empty);
                Log.Information("Cleared pending screenshots file after successful sync.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error clearing pending screenshots file.");
            }
        }
        private IEnumerable<string> GetPendingScreenshots()
        {
            if (!File.Exists(_pendingScreenshotFilePath))
            {
                Log.Information($"No pending screenshots");
                return new List<string>(); // No pending screenshots                
            }

            return File.ReadAllLines(_pendingScreenshotFilePath);
        }
    }
}
