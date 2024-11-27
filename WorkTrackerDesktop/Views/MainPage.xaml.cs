using WorkTrackerDesktop.Services;
using Microsoft.Maui.Controls;
using System.Timers;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System;

namespace WorkTrackerDesktop.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly WorkTimerService _workTimerService;
        private readonly OfflineSyncService _offlineSyncService;
        private readonly ScreenshotService _screenshotService;

        private System.Timers.Timer _workTimer;  // Timer for working time
        private int _elapsedTimeInSeconds = 0;  // Total working time
        private System.Timers.Timer _pauseTimer;  // Timer for paused time (either break or meeting)
        private int _pausedTimeInSeconds = 0;  // Total paused time
        private bool _isPaused = false;  // Is the user paused
        private string _selectedPauseType;  // Type of pause (Break or Meeting)
        public ObservableCollection<string> PauseTypes { get; set; }  // Available pause types
        public string SelectedPauseType { get; set; }  // Selected pause type from the UI
        private string _greetingMessage = "";  // Greeting message to display

        public MainPage()
        {
            InitializeComponent();
            _authService = new AuthService(new HttpClient());
            _workTimerService = new WorkTimerService();
            _offlineSyncService = new OfflineSyncService();
            _screenshotService = new ScreenshotService();
            PauseTypes = new ObservableCollection<string> { "Break", "Meeting", "Bathroom", "Other" };
            _offlineSyncService.StartPeriodicSync(); // Sync every 5 minutes
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            // Initialize the work timer if not already initialized
            if (_workTimer == null)
            {
                _workTimer = new System.Timers.Timer(1000);  // 1000 ms = 1 second
                _workTimer.Elapsed += OnWorkTimerElapsed;
            }

            _workTimer.Start();  // Start work timer
            UpdateGreetingMessage();  // Update greeting message

            // Update UI for Start/Pause
            StartButton.IsVisible = false;
            PauseButton.IsVisible = true;
            StopButton.IsVisible = true;
            PausePicker.IsVisible = true;
            PausePicker.ItemsSource = PauseTypes;
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            if (PausePicker.SelectedItem == null)
            {
                DisplayAlert("Error", "Please select a pause type before pausing.", "OK");
                return;
            }

            _selectedPauseType = PausePicker.SelectedItem.ToString();

            // Pause work timer and start pause timer
            _workTimer.Stop();
            _isPaused = true;

            if (_pauseTimer == null)
            {
                _pauseTimer = new System.Timers.Timer(1000);  // 1000 ms = 1 second
                _pauseTimer.Elapsed += OnPauseTimerElapsed;
            }

            _pauseTimer.Start();  // Start the pause timer

            // Update UI for Pause/Resume
            PauseButton.IsVisible = false;
            PausePicker.IsVisible = false;
            ResumeButton.IsVisible = true;
        }
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var responseMessage = await _authService.LogoutAsync();

            if (responseMessage.IsSuccessStatusCode)
            {
                // Navigate to MainPage (Work Tracking)
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
        private void OnResumeClicked(object sender, EventArgs e)
        {
            _workTimer.Start();  // Resume work timer
            _isPaused = false;

            // Stop the pause timer and reset paused time
            _pauseTimer.Stop();

            // Update UI for Pause/Resume
            ResumeButton.IsVisible = false;
            PauseButton.IsVisible = true;
            PausePicker.IsVisible = true;
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Confirm", "Are you sure you want to stop your work for today?", "Yes", "No");
            if (confirm)
            {
                // Display message upon confirmation
                await DisplayAlert("Work Completed", "You've completed your work today. Great job!", "OK");

                _workTimer.Stop();  // Stop the work timer

                TimeSpan workTime = TimeSpan.FromSeconds(_elapsedTimeInSeconds);
                WorkTimerLabel.Text = workTime.ToString(@"hh\:mm\:ss");

                TimeSpan pauseTime = TimeSpan.FromSeconds(_pausedTimeInSeconds);
                WorkTimerLabel.Text = pauseTime.ToString(@"hh\:mm\:ss");

                _elapsedTimeInSeconds = 0;  // Reset work time
                _pausedTimeInSeconds = 0;  // Reset paused time

                // Update UI to show total time worked and paused
                StopButton.IsVisible = false;
                PauseButton.IsVisible = false;
                PausePicker.IsVisible = false;
                FinishedWorkMessage.IsVisible = true;

            }
        }

        private void OnWorkTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Increment elapsed time for work timer
            _elapsedTimeInSeconds++;

            // Update the UI in the main thread
            Device.BeginInvokeOnMainThread(() =>
            {
                TimeSpan time = TimeSpan.FromSeconds(_elapsedTimeInSeconds);
                WorkTimerLabel.Text = time.ToString(@"hh\:mm\:ss");
            });
        }

        private void OnPauseTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Increment paused time
            _pausedTimeInSeconds++;

            // Update the UI in the main thread
            Device.BeginInvokeOnMainThread(() =>
            {
                TimeSpan time = TimeSpan.FromSeconds(_pausedTimeInSeconds);
                PauseTimerLabel.Text = time.ToString(@"hh\:mm\:ss");
            });
        }

        private void UpdateGreetingMessage()
        {
            // Get current hour of the day
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
            // Display greeting message on UI
            GreetingMessage.Text = _greetingMessage;
        }

        private async void OnViewReportClicked(object sender, EventArgs e)
        {
            Uri uri = new Uri("https://your-web-app-url.com/WorkReport");
            await Launcher.OpenAsync(uri);
        }
    }
}
