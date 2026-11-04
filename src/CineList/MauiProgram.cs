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

		// Las conexiones reutilizadas se renuevan: tras un rato sin uso (p. ej. con la cámara o el selector
		// de fotos abiertos) el servidor o la red las cierran y la siguiente petición fallaría con "Socket closed"
		builder.Services.AddSingleton(new HttpClient(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(2),
			PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
			ConnectTimeout = TimeSpan.FromSeconds(10),
		})
		{ Timeout = TimeSpan.FromSeconds(20) });
		builder.Services.AddSingleton<IPeliculasRepository>(sp => new PeliculasRepository(sp.GetRequiredService<HttpClient>()));
		builder.Services.AddSingleton<TemaService>();
		builder.Services.AddSingleton<FiltrosGuardados>();
		builder.Services.AddSingleton<PeliculasViewModel>();
		builder.Services.AddTransient<PeliculasPage>();
		builder.Services.AddTransient<DetalleViewModel>();
		builder.Services.AddTransient<DetallePage>();
		builder.Services.AddSingleton<PosterService>();
		builder.Services.AddTransient<FormularioViewModel>();
		builder.Services.AddTransient<FormularioPage>();

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

		// Campos del formulario sin la línea inferior nativa: el borde lo dibuja el Border que los envuelve
		var sinLinea = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinLinea", (handler, _) =>
			handler.PlatformView.BackgroundTintList = sinLinea);
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("SinLinea", (handler, _) =>
			handler.PlatformView.BackgroundTintList = sinLinea);
		Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("SinLinea", (handler, _) =>
			handler.PlatformView.BackgroundTintList = sinLinea);
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
