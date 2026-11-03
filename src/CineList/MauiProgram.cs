using CineList.Core.Firestore;
using CineList.Services;
using CineList.Views;
using Microsoft.Extensions.Logging;

namespace CineList;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Inter-Regular.ttf", "Inter");
				fonts.AddFont("Inter-SemiBold.ttf", "InterSemi");
				fonts.AddFont("Inter-Bold.ttf", "InterBold");
				fonts.AddFont("Fraunces-Bold.ttf", "Fraunces");
				fonts.AddFont("MaterialSymbolsRounded.ttf", "Iconos");
				fonts.AddFont("MaterialSymbolsRoundedFill.ttf", "IconosRellenos");
			});

		builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
		builder.Services.AddSingleton<IPeliculasRepository>(sp => new PeliculasRepository(sp.GetRequiredService<HttpClient>()));
		builder.Services.AddSingleton<TemaService>();
		builder.Services.AddTransient<PeliculasPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
