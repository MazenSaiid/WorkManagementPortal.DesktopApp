using WorkTrackerDesktop.Services;
using Microsoft.Maui.Controls;
using System.Text.RegularExpressions;

namespace WorkTrackerDesktop.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage()
    {
        InitializeComponent();
        _authService = new AuthService(new HttpClient());
    }
    private void OnEyeButtonClicked(object sender, EventArgs e)
    {
        // Toggle the IsPassword property of the PasswordEntry
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // Optionally change the icon based on the password visibility
        if (PasswordEntry.IsPassword)
        {
            EyeButton.Source = "eye_icon.png"; // Eye closed
        }
        else
        {
            EyeButton.Source = "eye_open_icon.png"; // Eye opened
        }
    }
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Validation Error", "Email and Password cannot be empty", "OK");
            return;
        }
        if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            await DisplayAlert("Invalid Email", "Please enter a valid email address.", "OK");
            return;
        }
        var responseMessage = await _authService.LoginAsync(email, password);

        if (responseMessage.IsSuccessStatusCode)
        {
            // Navigate to MainPage (Work Tracking)
            await Shell.Current.GoToAsync("//MainPage");
        }
        else
        {
            await DisplayAlert("Login Failed", "Invalid email or password", "OK");
        }
    }

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        // Show a prompt to the user for email input to reset the password
        string email = await DisplayPromptAsync("Forgot Password",
                                                 "Please enter your email address to reset your password.",
                                                 placeholder: "Email",
                                                 keyboard: Keyboard.Email);
        if (email == null)
        {
            return; // Just return if the user pressed cancel
        }

        // If the email is not empty or whitespace, proceed with the password reset
        if (!string.IsNullOrWhiteSpace(email))
        {
            // Call your service to handle password reset logic
            var response = await _authService.ResetPasswordAsync(email);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Password reset instructions have been sent to your email.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "There was an issue with the password reset. Please try again.", "OK");
            }
        }
        else
        {
            // If the email is empty or whitespace, show an error message
            await DisplayAlert("Error", "Email address cannot be empty.", "OK");
        }
    }

}
