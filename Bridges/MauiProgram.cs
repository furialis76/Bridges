using Bridges.Models;
using Bridges.Drawables;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace Bridges
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<SharedData>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<CreateGame>();
            builder.Services.AddSingleton<GameManager>();
            builder.Services.AddSingleton<Field>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
