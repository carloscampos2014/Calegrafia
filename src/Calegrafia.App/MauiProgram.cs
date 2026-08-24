using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Calegrafia.App.Services;
using Calegrafia.App.ViewModels.Auth;
using Calegrafia.App.Views.Auth;

namespace Calegrafia.App;

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

        // -------------------------------------------------------------------------
        // Configuration — load appsettings.json from embedded resource
        // -------------------------------------------------------------------------
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("Calegrafia.App.appsettings.json");
        if (stream is not null)
            ((IConfigurationBuilder)builder.Configuration).AddJsonStream(stream);

        // -------------------------------------------------------------------------
        // HTTP client — typed client for AuthApiService
        // -------------------------------------------------------------------------
        var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001";

        builder.Services.AddHttpClient<AuthApiService>(client =>
            client.BaseAddress = new Uri(baseUrl));

        // -------------------------------------------------------------------------
        // Services
        // -------------------------------------------------------------------------
        builder.Services.AddSingleton<IAuthApiService, AuthApiService>();

        // -------------------------------------------------------------------------
        // ViewModels
        // -------------------------------------------------------------------------
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<CadastroViewModel>();
        builder.Services.AddTransient<RecuperarSenhaViewModel>();
        builder.Services.AddTransient<MfaViewModel>();

        // -------------------------------------------------------------------------
        // Pages
        // -------------------------------------------------------------------------
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CadastroPage>();
        builder.Services.AddTransient<RecuperarSenhaPage>();
        builder.Services.AddTransient<MfaPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
