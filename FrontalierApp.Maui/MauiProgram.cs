using FrontalierApp.Services;
using Microsoft.Extensions.Logging;

namespace FrontalierApp.Maui;

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
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<IStorageService, MauiStorageService>();
        builder.Services.AddSingleton(sp => new HttpClient());
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<SupabaseStorageService>();
        builder.Services.AddSingleton<TeleworkService>();
        builder.Services.AddSingleton<LangService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
