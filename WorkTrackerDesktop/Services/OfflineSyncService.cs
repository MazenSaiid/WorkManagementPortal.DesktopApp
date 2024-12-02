using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation; // For checking internet connection
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace WorkTrackerDesktop.Services
{
    public class OfflineSyncService
    {
        private readonly HttpClient _httpClient;
        private const string ScreenshotUrl = "https://localhost:7119/api/ScreenshotTrackings/";
        private System.Timers.Timer _syncTimer;

        public OfflineSyncService()
        {
            _httpClient = new HttpClient();
            _syncTimer = new System.Timers.Timer(60000); // Sync every minute (60 seconds)
            _syncTimer.Elapsed += async (sender, e) => await CaptureAndSyncScreenshotAsync(); // Capture and sync every minute
        }

        public void StartPeriodicSync()
        {
            _syncTimer.Start();
        }

        // Method to check if there is an active internet connection
        private bool IsInternetAvailable()
        {
            try
            {
                // Check if any network is available
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        // This method will capture the screenshot and sync it (upload or save for later)
        private async Task CaptureAndSyncScreenshotAsync()
        {
            try
            {
                // Capture a screenshot every minute
                string screenshotPath = CaptureScreenshot();

                // Check if internet is available
                if (IsInternetAvailable())
                {
                    // If internet is available, upload the screenshot
                    await UploadScreenshotData(screenshotPath);
                }
                else
                {
                    // If no internet, save the screenshot for later upload
                    SaveScreenshotForLater(screenshotPath);
                }

                // Sync any pending screenshots (those saved when offline)
                await SyncPendingScreenshots();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during periodic sync: {ex.Message}");
            }
        }

        // Upload the screenshot to the server
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
                    File.Delete(screenshotPath); // Delete the screenshot after successful upload
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

        // Save the screenshot for later upload when there's no internet
        private void SaveScreenshotForLater(string screenshotPath)
        {
            string pendingFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pending_screenshots.txt");
            File.AppendAllText(pendingFilePath, screenshotPath + Environment.NewLine);
            Console.WriteLine($"Screenshot saved for later upload: {screenshotPath}");
        }

        // Sync the pending screenshots (upload all when the internet is available)
        private async Task SyncPendingScreenshots()
        {
            try
            {
                var screenshotPaths = GetPendingScreenshots(); // Read paths of pending screenshots
                foreach (var screenshotPath in screenshotPaths)
                {
                    if (IsInternetAvailable())
                    {
                        // If internet is available, upload the screenshot
                        await UploadScreenshotData(screenshotPath);

                        // After successful upload, delete the local file
                        DeleteLocalScreenshot(screenshotPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing pending screenshots: {ex.Message}");
            }
        }

        // Get the list of paths of all pending screenshots
        private IEnumerable<string> GetPendingScreenshots()
        {
            string pendingFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pending_screenshots.txt");
            if (!File.Exists(pendingFilePath))
            {
                return new List<string>(); // No pending screenshots
            }

            return File.ReadAllLines(pendingFilePath);
        }

        // Delete the screenshot file after upload
        private void DeleteLocalScreenshot(string screenshotPath)
        {
            try
            {
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath); // Delete the screenshot file after successful upload
                    Console.WriteLine($"Local screenshot file deleted: {screenshotPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting local screenshot: {ex.Message}");
            }
        }

        // Capture a screenshot and save it locally
        public string CaptureScreenshot()
        {
            try
            {
                var screenWidth = Screen.PrimaryScreen.Bounds.Width;
                var screenHeight = Screen.PrimaryScreen.Bounds.Height;
                // Capture the screenshot of the primary screen
                using (Bitmap screenshot = new Bitmap(screenWidth, screenHeight))
                {
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
                    }

                    // Save the screenshot locally with a timestamp
                    string screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                    screenshot.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);

                    Console.WriteLine($"Screenshot captured and saved to: {screenshotPath}");

                    return screenshotPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screenshot: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
