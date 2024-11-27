using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace WorkTrackerDesktop.Services
{
    public class ScreenshotService
    {
        private readonly HttpClient _httpClient;
        private const string UploadUrl = "https://your-api-endpoint.com/upload-screenshot"; // Change to your API endpoint

        public ScreenshotService()
        {
            _httpClient = new HttpClient();
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
                    await UploadScreenshotAsync(screenshotPath);
                }
                else
                {
                    // Save screenshot path locally for later upload
                    await SaveScreenshotLocallyAsync(screenshotPath);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions, possibly log the error
                Console.WriteLine($"Error capturing screenshot: {ex.Message}");
            }
        }

        private bool IsInternetAvailable()
        {
            // Check if the internet is available
            return NetworkInterface.GetIsNetworkAvailable(); // or other method to check network status
        }




private async Task SaveScreenshotToFileAsync(IScreenshotResult screenshotResult, string filePath)
    {
        try
        {
            // Assuming screenshotResult.OpenReadAsync() returns a Stream
            var screenshotStream = await screenshotResult.OpenReadAsync(); // Make sure this returns a Stream

            if (screenshotStream != null)
            {
                // Use ImageSharp to load the image from the stream
                using (var image = await Image.LoadAsync(screenshotStream)) // Load the image from the stream asynchronously

                {
                    // Save the image to the specified file path
                    await image.SaveAsync(filePath, new PngEncoder());  // Save as PNG
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving screenshot: {ex.Message}");
        }
    }



    private async Task UploadScreenshotAsync(string filePath)
        {
            try
            {
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(new FileStream(filePath, FileMode.Open));
                fileContent.Headers.Add("Content-Type", "image/png");
                content.Add(fileContent, "file", "screenshot.png");

                var response = await _httpClient.PostAsync(UploadUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Screenshot uploaded successfully.");
                    File.Delete(filePath); // Delete the local file after successful upload
                }
                else
                {
                    Console.WriteLine("Failed to upload screenshot.");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions related to the upload
                Console.WriteLine($"Error uploading screenshot: {ex.Message}");
            }
        }

        private async Task SaveScreenshotLocallyAsync(string filePath)
        {
            // You can store it in a local file or database for later upload
            var localStoragePath = Path.Combine(FileSystem.AppDataDirectory, "pending_screenshots.txt");
            await File.AppendAllTextAsync(localStoragePath, filePath + Environment.NewLine);
        }
    }
}
