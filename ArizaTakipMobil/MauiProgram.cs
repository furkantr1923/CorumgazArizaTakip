using System.Globalization;
using ArizaTakipMobil.Services;
using Microsoft.Extensions.Logging;

namespace ArizaTakipMobil;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Tarih ve sayılar Türkçe görünsün
        var kultur = new CultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = kultur;
        CultureInfo.DefaultThreadCurrentUICulture = kultur;
        CultureInfo.CurrentCulture = kultur;
        CultureInfo.CurrentUICulture = kultur;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // Uygulamanın servisleri (tek örnek, uygulama boyunca yaşar)
        builder.Services.AddSingleton<VeriServisi>();
        builder.Services.AddSingleton<OturumServisi>();
        builder.Services.AddSingleton<CihazServisi>();
        builder.Services.AddSingleton<AyarServisi>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
