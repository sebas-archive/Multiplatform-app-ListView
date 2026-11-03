using CineList.Core.Firestore;
using CineList.Services;
using CineList.ViewModels;
using CineList.Views;
using Microsoft.Extensions.Logging;
#if ANDROID
using Microsoft.Maui.Platform;
#endif

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
		builder.Services.AddSingleton<PeliculasViewModel>();
		builder.Services.AddTransient<PeliculasPage>();

#if ANDROID
		// SearchBar sin la línea inferior nativa y con la lupa en el color de texto secundario
		Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping(nameof(SearchBar.PlaceholderColor), (handler, vista) =>
		{
			var buscador = handler.PlatformView;
			buscador.FindViewById(Resource.Id.search_plate)?.SetBackgroundColor(Android.Graphics.Color.Transparent);
			if (buscador.FindViewById(Resource.Id.search_mag_icon) is Android.Widget.ImageView lupa
				&& vista.PlaceholderColor is { } color)
				lupa.SetColorFilter(color.ToPlatform());
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
