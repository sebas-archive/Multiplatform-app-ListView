#if ANDROID
using Microsoft.Maui.Platform;
#endif

namespace CineList.Services;

// Pinta la barra de estado y la de navegación de Android con el fondo del tema actual
// y elige íconos oscuros (tema claro) o claros (tema oscuro).
public static class BarraEstado
{
    public static void Aplicar(AppTheme tema)
    {
#if ANDROID
        var actividad = Platform.CurrentActivity;
        if (actividad?.Window is { } ventana) Pintar(ventana, tema);
        // Las páginas modales (formulario) viven en un diálogo con su propia ventana
        if (actividad is AndroidX.Fragment.App.FragmentActivity fa)
            foreach (var f in fa.SupportFragmentManager.Fragments)
                if (f is AndroidX.Fragment.App.DialogFragment { Dialog.Window: { } dialogo })
                    Pintar(dialogo, tema);
#endif
    }

    // Con el panel de filtros abierto, la barra de estado sigue al velo y la de navegación a la hoja,
    // para que no queden franjas sin oscurecer; con null se vuelve a los colores del tema
    public static void FondoSistema(Color? arriba, Color? abajo)
    {
#if ANDROID
        if (Platform.CurrentActivity?.Window is not { } ventana) return;
        if (arriba is null || abajo is null)
        {
            Pintar(ventana, Application.Current?.RequestedTheme ?? AppTheme.Dark);
            return;
        }
#pragma warning disable CA1422 // ver Pintar
        ventana.SetStatusBarColor(arriba.ToPlatform());
        ventana.SetNavigationBarColor(abajo.ToPlatform());
#pragma warning restore CA1422
#endif
    }

#if ANDROID
    private static void Pintar(Android.Views.Window ventana, AppTheme tema)
    {
        var oscuro = tema == AppTheme.Dark;
        var fondo = Android.Graphics.Color.ParseColor(oscuro ? "#0F1115" : "#F7F5F0");
#pragma warning disable CA1422 // obsoleto en Android 15 (allí la app es edge-to-edge y no hace falta)
        ventana.SetStatusBarColor(fondo);
        ventana.SetNavigationBarColor(fondo);
#pragma warning restore CA1422
        var controlador = AndroidX.Core.View.WindowCompat.GetInsetsController(ventana, ventana.DecorView);
        if (controlador is null) return;
        controlador.AppearanceLightStatusBars = !oscuro;
        controlador.AppearanceLightNavigationBars = !oscuro;
    }
#endif
}
