using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Linq;
using WorkTrackerDesktopWPFApp.Services;
using WorkTrackerDesktopWPFApp.Dtos;
using System.Windows.Forms;

namespace WorkTrackerWPFApp.Views
{
    public partial class WorkTracking : Window
    {
        private readonly AuthService _authService;
        private readonly WorkTimerService _workTimerService;
        private readonly ScreenshotService _screenshotService;
        private System.Timers.Timer _workTimer;  // Timer for working time
        private int _elapsedTimeInSeconds = 0;  // Total working time
        private System.Timers.Timer _pauseTimer;  // Timer for paused time (either break or meeting)
        private int _pausedTimeInSeconds = 0;  // Total paused time
        public ObservableCollection<PauseTypeDto> PauseTypes { get; set; }  // Available pause types
        public string SelectedPauseType { get; set; }  // Selected pause type from the UI
        private string _greetingMessage = "";  // Greeting message to display
        private bool IsWorkStarted = false;
        private bool IsWorkEnded = false;
        private readonly IConfiguration _configuration;

        public WorkTracking()
        {
            InitializeComponent();

            // Dynamically find the current directory
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configFilePath = System.IO.Path.Combine(currentDirectory, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddJsonFile(configFilePath, optional: false, reloadOnChange: true)
                .Build();

            _authService = new AuthService(new HttpClient(), _configuration);
            _workTimerService = new WorkTimerService(_configuration);
            _screenshotService = new ScreenshotService(_configuration);

            PauseTypes = new ObservableCollection<PauseTypeDto>();
            _screenshotService.StartPeriodicSync(); // Sync every minute
            
            // Initialize the username and role labels
            UsernameLabel.Content = UserSessionService.Instance.Username;
            RoleLabel.Content = UserSessionService.Instance.FirstRole;// Register the Closing event handler
            this.Closing += Window_Closing;
            LoadPauseTypes();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Show a confirmation dialog before closing
            var result = System.Windows.MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            // If the user clicks "No", cancel the close event
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                // Clear work session and user session
                WorkSessionService.Instance.ClearWorkSession();
                UserSessionService.Instance.ClearSession();

                // Shut down the application
                System.Windows.Application.Current.Shutdown();
            }
        }

        private async void LoadPauseTypes()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var pauseTypesUrl = _configuration["ApiBaseUrl"] + "WorkTrackings/GetPauseTypes";
                    var response = await client.GetAsync(pauseTypesUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var pauseTypes = JsonConvert.DeserializeObject<List<PauseTypeDto>>(jsonResponse);

                        PauseTypes.Clear();
                        foreach (var pauseType in pauseTypes)
                        {
                            PauseTypes.Add(pauseType);
                        }

                        PausePicker.ItemsSource = PauseTypes;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to load pause types. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("There was an issue with the loading pause types. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error loading pause types");
            }
        }

        private async void OnStartClicked(object sender, RoutedEventArgs e)
        {
            // Initialize the work timer if not already initialized
            if (_workTimer == null)
            {
                _workTimer = new System.Timers.Timer(1000);  // 1000 ms = 1 second
                _workTimer.Elapsed += OnWorkTimerElapsed;
            }

            var response = await _workTimerService.StartAsync();
            if (response.Success)
            {
                _workTimer.Start();  // Start work timer
                UpdateGreetingMessage();  // Update greeting message

                // Update UI for Start/Pause
                StartButton.Visibility = Visibility.Hidden;
                PauseButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Visible;
                PausePicker.Visibility = Visibility.Visible;

                WorkSessionService.Instance.WorkLogId = response.WorkTrackingLog.Id;
                WorkSessionService.Instance.WorkTimeStart = response.WorkTrackingLog.WorkTimeStart;

                // Set work start time on the label
                WorkStartTimeLabel.Content = "Start Time: " + WorkSessionService.Instance.WorkTimeStart.ToString("hh:mm:ss tt");

                // Update state variables
                IsWorkStarted = true;

                // Update visibility based on the start
                WorkStartTimeLabel.Visibility = Visibility.Visible;  // Show start time
            }
            else
            {
                System.Windows.MessageBox.Show("There was an issue with the Starting work session. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnPauseClicked(object sender, RoutedEventArgs e)
        {
            if (PausePicker.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Please select a pause type before pausing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedPauseType = (PauseTypeDto)PausePicker.SelectedItem;
            var response = await _workTimerService.PauseAsync(selectedPauseType.Id);

            if (response.Success)
            {
                // Pause work timer and start the pause timer
                _workTimer.Stop();  // Stop work timer

                // Start a new pause timer if not already initialized
                if (_pauseTimer == null)
                {
                    _pauseTimer = new System.Timers.Timer(1000);  // 1000 ms = 1 second
                    _pauseTimer.Elapsed += OnPauseTimerElapsed;
                }

                _pauseTimer.Start();  // Start the pause timer

                // Show pause message
                PauseMessageLabel.Content = $"You are currently on a pause: {selectedPauseType.Name}";
                PauseMessageLabel.Visibility = Visibility.Visible;

                // Update UI for Pause/Resume
                PauseButton.Visibility = Visibility.Hidden;
                PausePicker.Visibility = Visibility.Hidden;
                ResumeButton.Visibility = Visibility.Visible;
            }
            else
            {
                System.Windows.MessageBox.Show("There was an issue with Pausing. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                // Clear work session and user session
                WorkSessionService.Instance.ClearWorkSession();
                UserSessionService.Instance.ClearSession();

                // Log the action to ensure it’s working
                Debug.WriteLine("User logged out, navigating to Login page.");
                this.Hide();
                var LoginPage = new Login();
                LoginPage.Show();

            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "Error in Logout");
                Debug.WriteLine($"Error in Logout: {ex.Message}");
            }
        }
        private async void OnResumeClicked(object sender, RoutedEventArgs e)
        {
            var response = await _workTimerService.ResumeAsync();
            if (response.Success)
            {
                _workTimer.Start();  // Resume work timer
                PauseMessageLabel.Visibility = Visibility.Hidden;

                // Stop the pause timer and reset paused time
                _pauseTimer.Stop();

                // Update UI for Pause/Resume
                ResumeButton.Visibility = Visibility.Hidden;
                PauseButton.Visibility = Visibility.Visible;
                PausePicker.Visibility = Visibility.Visible;
            }
            else
            {
                System.Windows.MessageBox.Show("There was an issue with Resuming. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnStopClicked(object sender, RoutedEventArgs e)
        {
            var confirm = System.Windows.MessageBox.Show("Are you sure you want to stop your work for today?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                var response = await _workTimerService.StopAsync();
                if (response.Success)
                {
                    _workTimer.Stop();  // Stop the work timer
                    if (_pauseTimer != null)
                    {
                        _pauseTimer.Stop(); // Stop the pause timer
                    }

                    // Calculate total time worked
                    TimeSpan workTime = TimeSpan.FromSeconds(_elapsedTimeInSeconds);
                    WorkTimerLabel.Content = workTime.ToString(@"hh\:mm\:ss");

                    // Calculate total pause time
                    TimeSpan pauseTime = TimeSpan.FromSeconds(_pausedTimeInSeconds);
                    PauseTimerLabel.Content = pauseTime.ToString(@"hh\:mm\:ss");

                    // Reset time counters
                    _elapsedTimeInSeconds = 0;
                    _pausedTimeInSeconds = 0;

                    WorkSessionService.Instance.WorkTimeEnd = response.WorkTrackingLog.WorkTimeEnd;

                    // Set work end time on the label
                    WorkEndTimeLabel.Content = "End Time: " + WorkSessionService.Instance.WorkTimeEnd.ToString("hh:mm:ss tt");

                    // Update state variables
                    IsWorkStarted = false;
                    IsWorkEnded = true;
                    WorkEndTimeLabel.Visibility = Visibility.Visible;
                    PauseMessageLabel.Visibility = Visibility.Hidden;

                    // Hide buttons and show finished message
                    StopButton.Visibility = Visibility.Hidden;
                    PauseButton.Visibility = Visibility.Hidden;
                    PausePicker.Visibility = Visibility.Hidden;
                    ResumeButton.Visibility = Visibility.Hidden;
                    FinishedWorkMessage.Visibility = Visibility.Visible;

                    // Display a completion message
                    System.Windows.MessageBox.Show("You've completed your work today. Great job!", "Work Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("There was an issue with stopping your session. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnWorkTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _elapsedTimeInSeconds++;

            Dispatcher.Invoke(() =>
            {
                TimeSpan time = TimeSpan.FromSeconds(_elapsedTimeInSeconds);
                WorkTimerLabel.Content = time.ToString(@"hh\:mm\:ss");
            });
        }

        private void OnPauseTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _pausedTimeInSeconds++;

            Dispatcher.Invoke(() =>
            {
                TimeSpan time = TimeSpan.FromSeconds(_pausedTimeInSeconds);
                PauseTimerLabel.Content = time.ToString(@"hh\:mm\:ss");
            });
        }

        private void UpdateGreetingMessage()
        {
            int currentHour = DateTime.Now.Hour;

            if (currentHour >= 5 && currentHour < 12)
            {
                _greetingMessage = "Good Morning!";
            }
            else if (currentHour >= 12 && currentHour < 18)
            {
                _greetingMessage = "Good Afternoon!";
            }
            else
            {
                _greetingMessage = "Good Evening!";
            }

            GreetingMessage.Content = _greetingMessage;
        }

        private async void OnViewReportClicked(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri(_configuration["FrontEndUrl"] ?? "https://localhost:4200/" + "core/my-work");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
