namespace CineList.Helpers;

// Posición de un elemento en coordenadas de ventana (unidades MAUI), medida en la vista nativa.
// Sirve para relacionar elementos de páginas distintas (lista → detalle) sin depender de la
// cadena de Bounds de MAUI, que no atraviesa bien el ListView. Fuera de Android devuelve null.
public static class Coordenadas
{
    public static Rect? EnVentana(VisualElement elemento)
    {
#if ANDROID
        if (elemento.Handler?.PlatformView is not Android.Views.View nativa
            || !nativa.IsAttachedToWindow || nativa.Width <= 0 || nativa.Height <= 0)
            return null;
        var posicion = new int[2];
        nativa.GetLocationInWindow(posicion);
        var densidad = nativa.Resources?.DisplayMetrics?.Density ?? 1f;
        return new Rect(posicion[0] / densidad, posicion[1] / densidad,
            nativa.Width / densidad, nativa.Height / densidad);
#else
        return null;
#endif
    }
}
