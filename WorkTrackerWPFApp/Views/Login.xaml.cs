﻿using System;
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
    public partial class Login : Window
    {
        private readonly AuthService _authService;
        private IConfiguration _configuration;
        public Login()
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
        private string GetPassword()
        {
            // Get the password from the correct control
            return PasswordBox.Visibility == Visibility.Visible
                ? PasswordBox.Password
                : PasswordTextBox.Text;
        }
        private void OnEyeButtonClicked(object sender, RoutedEventArgs e)
        {
            if (PasswordTextBox.Visibility == Visibility.Collapsed) // Password is hidden, show it
            {
                // Show the TextBox and hide PasswordBox
                PasswordTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;

                // Set the password from PasswordBox to TextBox (in plain text)
                PasswordTextBox.Text = PasswordBox.Password;

                // Update the eye icon to "open-eye" (password is visible)
                EyeButton.Content = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/eye_open_icon.png"))
                };
            }
            else // Password is visible, hide it
            {
                // Show the PasswordBox and hide TextBox
                PasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;

                // Set the password back from TextBox to PasswordBox
                PasswordBox.Password = PasswordTextBox.Text;

                // Update the eye icon to "closed-eye" (password is hidden)
                EyeButton.Content = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/eye_icon.png"))
                };
            }
        }
        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            ClearError();
            string email = EmailTextBox.Text;
            string password = GetPassword();


            // Validate email and password
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                //ErrorMessageResponse.ShowErrorMessage("Email and Password cannot be empty", "Validation Error");
                ShowError("Email and Password cannot be empty");
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                //ErrorMessageResponse.ShowErrorMessage("Please enter a valid email address.", "Invalid Email");
                ShowError("Please enter a valid email address.");
                return;
            }
            try
            {
                var response = await _authService.LoginAsync(email, password);

                if (response.Success)
                {
                    UserSessionService.Instance.Username = response.Username;
                    UserSessionService.Instance.UserId = response.UserId;
                    UserSessionService.Instance.SessionExpiration = response.LocalSessionExpireDate;
                    UserSessionService.Instance.Token = response.Token;
                    UserSessionService.Instance.Roles = response.Roles;

                    var mainPage = new WorkTracking();
                    mainPage.Show();
                    // Close this window and open the main window or dashboard
                    this.Hide();
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
        private async void OnForgotPasswordClicked(object sender, RoutedEventArgs e)
        {
            ForgotPassword forgotPasswordPage = new ForgotPassword();
            forgotPasswordPage.Show();
            this.Hide();
        }
        private void ClearError()
        {
            // Clear any existing error message before proceeding
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            ErrorMessageTextBlock.Text = string.Empty;
        }
        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }
        private void ShowSuccess(string message)
        {
            SuccessMessageTextBlock.Text = message;
            SuccessMessageTextBlock.Visibility = Visibility.Visible;
        }
    }
}

