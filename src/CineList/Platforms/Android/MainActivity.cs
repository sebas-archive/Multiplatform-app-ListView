using Android.App;
using Android.Content.PM;
using Android.OS;
using CineList.Services;

namespace CineList;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		BarraEstado.Aplicar(Microsoft.Maui.Controls.Application.Current?.RequestedTheme ?? AppTheme.Dark);

		// Al cerrar un modal (formulario), Android devuelve el foco al RecyclerView de Shell, que es
		// focusableInTouchMode y no tiene estado "enfocado" propio: el sistema le dibuja el resaltado
		// de foco por defecto, un velo gris (~8 %) sobre toda la página. Se desactiva en contenedores;
		// botones y campos conservan su resaltado para quien navega con teclado.
		if (Window?.DecorView.ViewTreeObserver is { } observador)
			observador.GlobalFocusChange += (_, e) =>
			{
				if (OperatingSystem.IsAndroidVersionAtLeast(26) && e.NewFocus is Android.Views.ViewGroup contenedor)
					contenedor.DefaultFocusHighlightEnabled = false;
			};
	}
}
