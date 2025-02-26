using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using WorkTrackerDesktopWPFApp.Services;
using WorkTrackerWPFApp.Views;
using System.Windows.Media.Imaging;
using WorkTrackerWPFApp.Responses;
using WorkTrackerWPFApp.Services.Static;

namespace WorkTrackerWPFApp
{
    public partial class ForgotPassword : Window
    {
        private readonly AuthService _authService;
        private IConfiguration _configuration;
        public ForgotPassword()
        {
            InitializeComponent();
            // Dynamically find the current directory
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configFilePath = System.IO.Path.Combine(currentDirectory, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)  // Dynamically set the base path to the current directory
                .AddJsonFile(configFilePath, optional: false, reloadOnChange: true)  // Use the dynamically created path
                .Build();
            this.Closing += Window_Closing;
            _authService = new AuthService(new HttpClient(), _configuration);
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
        private async void OnSubmitClicked(object sender, RoutedEventArgs e)
        {
            ClearError();
            var email = EmailTextBox.Text;
            if (string.IsNullOrEmpty(email) )
            {   
                ShowError("Email cannot be empty");
                return;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ShowError("Please enter a valid email address.");
                    return;
                }
                try
                {
                    var response = await _authService.ResetPasswordAsync(email);
                    if (response.Success)
                    {
                        ShowSuccess(response.Message);
                    }
                    else
                    {
                        ShowError($"{response.Message}");
                    }
                }
                catch (Exception ex)
                {

                    ShowError($"{ex.Message}");
                }
            }
            else
            {
                ShowError("Email address cannot be empty.");
            }
        }
        private void OnBackToLoginClicked(object sender, RoutedEventArgs e)
        {
            GoBackToLogin();
        }
        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }
        private void ClearError()
        {
            // Clear any existing error message before proceeding
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            ErrorMessageTextBlock.Text = string.Empty;
        }
        private void ShowSuccess(string message)
        {
            SuccessMessageTextBlock.Text = message;
            SuccessMessageTextBlock.Visibility = Visibility.Visible;
        }

        // Navigate back to login page
        private void GoBackToLogin()
        {
            Login loginPage = new Login();
            loginPage.Show();
            this.Hide(); // Close the Forgot Password window
        }
    }
}

