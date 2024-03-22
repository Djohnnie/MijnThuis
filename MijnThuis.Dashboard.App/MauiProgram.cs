using Microsoft.Extensions.Logging;
using MijnThuis.Dashboard.App.Configuration;
using MijnThuis.Dashboard.App.Factories;

namespace MijnThuis.Dashboard.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<PageFactory>();
        builder.Services.AddSingleton<IClientConfiguration, ClientConfiguration>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}