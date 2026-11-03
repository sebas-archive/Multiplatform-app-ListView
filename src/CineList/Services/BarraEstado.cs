namespace CineList.Services;

// Pinta la barra de estado y la de navegación de Android con el fondo del tema actual
// y elige íconos oscuros (tema claro) o claros (tema oscuro).
public static class BarraEstado
{
    public static void Aplicar(AppTheme tema)
    {
#if ANDROID
        var ventana = Platform.CurrentActivity?.Window;
        if (ventana is null) return;
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
#endif
    }
}
