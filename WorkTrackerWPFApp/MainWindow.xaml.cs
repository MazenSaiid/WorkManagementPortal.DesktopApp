using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows;
using WorkTrackerWPFApp.Views;
using MahApps.Metro.Controls; // If you're using MetroWindow

namespace WorkTrackerWPFApp
{

    public partial class MainWindow : Window // Make sure it's a MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // Show the Login page
            var loginPage = new Login(); // Instantiate LoginPage
            loginPage.Show();  // Show LoginPage on startup

            // Close the current MainWindow since it's not needed
            this.Hide();  // Hide the MainWindow instead of closing it immediately
            // Register the Closing event handler
            this.Closing += Window_Closing;
        }

        // Closing event handler
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Show a confirmation dialog before closing
            var result = System.Windows.MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            // If the user clicks "No", cancel the close event
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

    }


}
