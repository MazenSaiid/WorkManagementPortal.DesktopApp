using System.Diagnostics;

namespace WorkTrackerDesktop;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Register for unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            // Log the exception or show a dialog box here
            Console.WriteLine("Unhandled exception: " + e.ExceptionObject.ToString());
            if (Debugger.IsAttached)
                Debugger.Break(); // Break in debugger
        };

        // For UI thread exceptions
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.WriteLine("Unhandled task exception: " + e.Exception);
            e.SetObserved(); // Mark the exception as observed to prevent app termination
            if (Debugger.IsAttached)
                Debugger.Break(); // Break in debugger
        };
    }
}

