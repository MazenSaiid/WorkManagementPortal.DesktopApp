using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerWPFApp.Responses
{
    public static class ErrorMessageResponse
    {
        public static async void ShowErrorMessage(string message, string title)
        {
            var metroWindow = System.Windows.Application.Current.MainWindow as MetroWindow;

            if (metroWindow == null)
            {
                MessageBox.Show("Unable to access the MetroWindow instance.");
                return;
            }

            // Ensure execution is on the UI thread
            await metroWindow.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await metroWindow.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, new MetroDialogSettings
                    {
                        ColorScheme = MetroDialogColorScheme.Accented
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error showing message: {ex.Message}");
                }
            });
        }


    }

}
