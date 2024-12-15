using Microsoft.Extensions.Logging;
using WorkTrackerDesktop;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using WorkTrackerDesktop.Services;

namespace WorkTrackerDesktop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            var configuration = builder.Configuration;

            // Load the default appsettings.json
            configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            configuration.AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true);

            // Register IConfiguration instance in DI container
            builder.Services.AddSingleton<IConfiguration>(configuration);

            // Register AuthService
            builder.Services.AddSingleton<AuthService>();

            builder.Services.AddSingleton<HttpClient>();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
