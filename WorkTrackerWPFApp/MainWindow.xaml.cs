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
using MahApps.Metro.Controls;
using WorkTrackerDesktopWPFApp.Services; // If you're using MetroWindow

namespace WorkTrackerWPFApp
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Show the Login page
            var loginPage = new Login(); // Instantiate LoginPage
            loginPage.Show();  // Show LoginPage on startup
            this.Hide();  // Hide the MainWindow instead of closing it immediately
        }

    }

}
