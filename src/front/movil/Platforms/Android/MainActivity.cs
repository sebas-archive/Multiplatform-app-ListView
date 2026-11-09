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

	// Pulsación larga (ContextActions): el ListView pinta la fila elegida con un fondo blanco a todo
	// lo ancho y la flecha de cerrar toma el color de control oscuro sobre la barra #0F1115.
	// Se deja la fila sin fondo (la tarjeta ya se ve sola) y la flecha en blanco.
	public override void OnSupportActionModeStarted(AndroidX.AppCompat.View.ActionMode? mode)
	{
		base.OnSupportActionModeStarted(mode);
		var raiz = Window?.DecorView;
		raiz?.Post(() =>
		{
			try
			{
				if (raiz.FindViewById(Resource.Id.action_mode_close_button) is Android.Widget.ImageView cerrar)
					cerrar.SetColorFilter(Android.Graphics.Color.White);
				LimpiarFondoFilas(raiz);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Warn("CineList", $"ActionMode: {ex.Message}");
			}
		});
	}

	private static void LimpiarFondoFilas(Android.Views.View? vista)
	{
		if (vista is Android.Widget.AbsListView lista)
		{
			lista.Selector = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Transparent);
			for (var i = 0; i < lista.ChildCount; i++)
				lista.GetChildAt(i)!.Background = null;
			return;
		}
		if (vista is Android.Views.ViewGroup grupo)
			for (var i = 0; i < grupo.ChildCount; i++)
				LimpiarFondoFilas(grupo.GetChildAt(i));
	}
}
