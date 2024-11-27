using Microsoft.Extensions.Logging;
using WorkTrackerDesktop;
using Microsoft.Extensions.Configuration;
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
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Access the configuration after DI setup
            //var apiBaseUrl = configuration["ApiBaseUrl"];

            // Register HttpClient with the ApiBaseUrl
            //builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
