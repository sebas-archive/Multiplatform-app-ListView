namespace CineList.Helpers;

// Desenfoque de una vista con RenderEffect (Android 12 / API 31 en adelante).
// En versiones anteriores no hace nada: el fondo queda sin desenfocar.
public static class Desenfoque
{
    public static void Aplicar(VisualElement elemento, float radio)
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;
        if (elemento.Handler?.PlatformView is not Android.Views.View nativa) return;
        var px = radio * (nativa.Resources?.DisplayMetrics?.Density ?? 1f);
        nativa.SetRenderEffect(Android.Graphics.RenderEffect.CreateBlurEffect(px, px, Android.Graphics.Shader.TileMode.Clamp!));
#endif
    }
}
