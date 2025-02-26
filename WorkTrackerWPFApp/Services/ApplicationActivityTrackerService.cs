using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;


namespace WorkTrackerWPFApp.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Net.Http;
    using WorkTrackerWPFApp.Dtos;
    using Serilog;
    using Microsoft.Extensions.Configuration;
    using System.Text.RegularExpressions;
    using System.Net.NetworkInformation;
    using WorkTrackerWPFApp.Services.Static;

    public class ApplicationActivityTrackerService
    {
        private static string _lastWindow = null; // Keeps track of the last active window title
        private static DateTime _startTime; // Tracks the start time of the current activity
        private readonly string _logFilePath; // File path for storing activity logs
        private readonly HttpClient _httpClient; // HttpClient for API interactions
        // Paths
        private readonly string _baseDirectory;
        private readonly string _uploadUrl; // API endpoint for sending logs
        private readonly string _username;

        public ApplicationActivityTrackerService(IConfiguration configuration)
        {
            _logFilePath = Path.Combine(PathHelperService.GetBaseDirectoryLogFilePath(configuration), $"ActivityLogs_{DateTime.Now:yyyy-MM-dd}.json");
            // Start tracking and periodic sending
            TrackUsage();

            // Read configuration settings
            _uploadUrl = configuration["ApiBaseUrl"];
            _httpClient = new HttpClient();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow(); // Retrieves the handle of the currently focused window

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count); // Gets the title of a window

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId); // Retrieves the process ID for a window

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            var Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString(); // Returns the title of the active window
            }
            return null; // Returns null if no title is available
        }

        public static string GetActiveProcessName()
        {
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out uint processId);

            try
            {
                var process = Process.GetProcessById((int)processId);
                return process.ProcessName; // Returns the name of the active process
            }
            catch
            {
                return null; // Returns null if the process cannot be retrieved
            }
        }
        public static string ExtractWebsiteFromBrowserTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return null;

            // Remove known browser suffixes
            string[] browserSuffixes =
            {
                " - Google Chrome",
                " - Microsoft Edge",
                " - Mozilla Firefox",
                " - Internet Explorer"
            };

            foreach (var suffix in browserSuffixes)
            {
                if (title.Contains(suffix))
                {
                    title = title.Replace(suffix, "").Trim();
                    break;
                }
            }

            // Check if the title contains a URL (e.g., "https://example.com")
            var urlPattern = @"https?:\/\/[^\s/$.?#].[^\s]*";
            var match = Regex.Match(title, urlPattern);
            if (match.Success)
                return new Uri(match.Value).Host; // Extract and return the host

            // If the title is in a "Website | Additional Info" format
            if (title.Contains(" | "))
                return title.Substring(0, title.IndexOf(" | ")).Trim();

            // If the title is in a "Additional Info - Website" format
            if (title.Contains(" - "))
                return title.Substring(title.LastIndexOf(" - ") + 3).Trim();

            // Return title as a fallback
            return title;
        }

        public async void TrackUsage()
        {
            while (true)
            {
                try
                {
                    string currentWindow = GetActiveWindowTitle();
                    string processName = GetActiveProcessName();
                    string website = "Local Application";

                    // Check if the current process is a browser
                    if (processName == "chrome" || processName == "msedge" || processName == "firefox")
                    {
                        // Attempt to extract website from browser title
                        website = ExtractWebsiteFromBrowserTitle(currentWindow);

                    }

                    if (_lastWindow != currentWindow)
                    {
                        if (!string.IsNullOrEmpty(_lastWindow))
                        {
                            var duration = DateTime.Now - _startTime;

                            AppendLog(new ActivityLog
                            {
                                ApplicationName = _lastWindow, // Logs the last window title
                                Website = website, // Logs the extracted website (if any)
                                StartTime = _startTime, // Logs the start time of the activity
                                EndTime = DateTime.Now, // Logs the end time of the activity
                                DurationInSeconds = duration.TotalSeconds // Calculates the duration in seconds
                            });
                        }

                        _lastWindow = currentWindow; // Updates the last window to the current one
                        _startTime = DateTime.Now; // Resets the start time
                    }

                    await Task.Delay(1000); // Checks every second
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error tracking usage"); // Logs any tracking errors
                }
            }
        }

        private void AppendLog(ActivityLog log)
        {
            var logs = new List<ActivityLog>();

            if (File.Exists(_logFilePath))
            {
                var existingLogs = File.ReadAllText(_logFilePath);
                logs = JsonConvert.DeserializeObject<List<ActivityLog>>(existingLogs) ?? new List<ActivityLog>();
            }

            logs.Add(log); // Adds the new log to the list
            File.WriteAllText(_logFilePath, JsonConvert.SerializeObject(logs)); // Writes the updated logs to the file
        }
        private bool IsInternetAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
        private List<ActivityLog> ReadLogsAggregated()
        {
            if (!File.Exists(_logFilePath))
            {
                return new List<ActivityLog>(); // Returns an empty list if no logs are found
            }

            var content = File.ReadAllText(_logFilePath);
            var logs = JsonConvert.DeserializeObject<List<ActivityLog>>(content) ?? new List<ActivityLog>();

            // Aggregates logs by application and website
            return logs
                .GroupBy(log => new { log.ApplicationName, log.Website })
                .Select(group => new ActivityLog
                {
                    ApplicationName = group.Key.ApplicationName,
                    Website = group.Key.Website,
                    StartTime = group.Min(log => log.StartTime), // Uses the earliest start time
                    EndTime = group.Max(log => log.EndTime), // Uses the latest end time
                    DurationInSeconds = group.Sum(log => log.DurationInSeconds) // Sums up the durations
                })
                .ToList();
        }

        private void ClearLogs()
        {
            if (File.Exists(_logFilePath))
            {
                File.Delete(_logFilePath); // Deletes the log file
            }
        }

        private async Task SendLogsToBackend()
        {
            var logs = ReadLogsAggregated();

            if (logs.Count > 0)
            {
                if (IsInternetAvailable())
                {
                    try
                    {
                        var userId = UserSessionService.Instance.UserId;
                        var workLogId = WorkSessionService.Instance.WorkLogId;
                        // Prepare the payload with UserId and WorkLogId
                        var payload = new
                        {
                            UserId = userId,
                            WorkLogId = workLogId,
                            Logs = logs
                        };

                        var content = new StringContent(
                            JsonConvert.SerializeObject(payload),
                            Encoding.UTF8,
                            "application/json"
                        );

                        var response = await _httpClient.PostAsync(_uploadUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            ClearLogs(); // Clears local logs upon successful API submission
                            Log.Information($"Logs sent to API and cleared from local storage at {DateTime.Now}");
                        }
                        else
                        {
                            Log.Warning("Failed to send logs to API. StatusCode: {StatusCode}", response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error sending logs to API"); // Logs any API submission errors
                    }
                }
            }
        }

        public void StartPeriodicSending(int intervalInMinutes)
        {
            System.Threading.Timer timer = new System.Threading.Timer(async _ => await SendLogsToBackend(), null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalInMinutes)); // Sets up periodic log sending
        }
    }
}
