namespace CineList.Helpers;

// Animaciones reutilizables (150-350 ms, Easing.CubicOut). Si el sistema pide reducir movimiento,
// las decorativas se saltan y los elementos quedan en su estado final.
public static class Animaciones
{
    private const uint Corta = 150;
    private const uint Media = 250;
    private const uint Larga = 350;

    public static bool ReducirMovimiento
    {
        get
        {
#if ANDROID
            try
            {
                // Android 8+: el estado real de los animadores del proceso (el mismo que usa MAUI para
                // animar); leer el ajuste puede no coincidir si se borró sin reiniciar el sistema
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    return !Android.Animation.ValueAnimator.AreAnimatorsEnabled();
                var resolver = Android.App.Application.Context.ContentResolver;
                return Android.Provider.Settings.Global.GetFloat(resolver, Android.Provider.Settings.Global.AnimatorDurationScale, 1f) == 0f;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }
    }

    // Cada elemento aparece con fade + sube 16 px, con retraso escalonado entre uno y otro
    public static async Task EntradaEscalonada(IEnumerable<VisualElement> elementos, uint retrasoMs = 40)
    {
        var lista = elementos.ToList();
        if (ReducirMovimiento)
        {
            foreach (var e in lista) { e.Opacity = 1; e.TranslationY = 0; }
            return;
        }
        foreach (var e in lista) { e.Opacity = 0; e.TranslationY = 16; }
        var tareas = new List<Task>();
        for (var i = 0; i < lista.Count; i++)
        {
            var e = lista[i];
            var retraso = (int)(i * retrasoMs);
            tareas.Add(Task.Run(async () =>
            {
                await Task.Delay(retraso);
                await MainThread.InvokeOnMainThreadAsync(() => Task.WhenAll(
                    e.FadeTo(1, Media, Easing.CubicOut),
                    e.TranslateTo(0, 0, Media, Easing.CubicOut)));
            }));
        }
        await Task.WhenAll(tareas);
    }

    // Tap en celda: se encoge a 0.97 y vuelve
    public static async Task Pulsar(VisualElement v)
    {
        if (ReducirMovimiento) return;
        await v.ScaleTo(0.97, 80, Easing.CubicOut);
        await v.ScaleTo(1, 100, Easing.CubicOut);
    }

    // Estrella: 1 -> 1.3 -> 1 (los chips del panel de filtros usan 1.15)
    public static async Task Pop(VisualElement v, double escala = 1.3)
    {
        if (ReducirMovimiento) return;
        await v.ScaleTo(escala, 120, Easing.CubicOut);
        await v.ScaleTo(1, 120, Easing.CubicOut);
    }

    // Detalle: el poster escala de 0.9 a 1 con fade; el resto aparece escalonado
    public static async Task EntradaDetalle(VisualElement poster, IEnumerable<VisualElement> resto)
    {
        if (ReducirMovimiento)
        {
            poster.Opacity = 1; poster.Scale = 1;
            foreach (var e in resto) { e.Opacity = 1; e.TranslationY = 0; }
            return;
        }
        poster.Opacity = 0;
        poster.Scale = 0.9;
        var restoTarea = EntradaEscalonada(resto, 50);
        await Task.WhenAll(
            poster.FadeTo(1, Larga, Easing.CubicOut),
            poster.ScaleTo(1, Larga, Easing.CubicOut),
            restoTarea);
    }

    // Eliminar: se desvanece y colapsa su altura
    public static async Task DesvanecerYColapsar(VisualElement v)
    {
        if (ReducirMovimiento) return;
        await v.FadeTo(0, Corta, Easing.CubicOut);
        var altura = v.Height;
        if (altura <= 0) return;
        var tcs = new TaskCompletionSource();
        var animacion = new Animation(f => v.HeightRequest = Math.Max(0, altura * (1 - f)), 0, 1, Easing.CubicOut);
        animacion.Commit(v, "Colapsar", 16, Media, finished: (_, _) => tcs.TrySetResult());
        await tcs.Task;
    }

    // Boton +: gira 90 grados
    public static async Task RotarFab(VisualElement v)
    {
        if (ReducirMovimiento) return;
        await v.RotateTo(v.Rotation + 90, Media, Easing.CubicOut);
        v.Rotation %= 360;
    }

    // Aparece con escala (0 -> 1) al cargar la pagina
    public static async Task AparecerEscala(VisualElement v)
    {
        if (ReducirMovimiento) { v.Scale = 1; v.Opacity = 1; return; }
        v.Scale = 0;
        v.Opacity = 0;
        await Task.WhenAll(
            v.ScaleTo(1, Larga, Easing.CubicOut),
            v.FadeTo(1, Larga, Easing.CubicOut));
    }

    // Cambio de etiqueta: leve rebote
    public static async Task Rebote(VisualElement v)
    {
        if (ReducirMovimiento) return;
        await v.ScaleTo(1.12, 100, Easing.CubicOut);
        await v.ScaleTo(1, 140, Easing.CubicOut);
    }

    // Formulario: entra desde abajo (40 px) con fade; luego sus bloques aparecen escalonados
    public static async Task EntradaModal(VisualElement contenido, IEnumerable<VisualElement> bloques)
    {
        var lista = bloques.ToList();
        if (ReducirMovimiento)
        {
            contenido.Opacity = 1; contenido.TranslationY = 0;
            foreach (var e in lista) { e.Opacity = 1; e.TranslationY = 0; }
            return;
        }
        contenido.Opacity = 0;
        contenido.TranslationY = 40;
        foreach (var e in lista) { e.Opacity = 0; e.TranslationY = 16; }
        await Task.WhenAll(
            contenido.FadeTo(1, Media, Easing.CubicOut),
            contenido.TranslateTo(0, 0, Larga, Easing.CubicOut),
            EntradaEscalonada(lista, 35));
    }

    // Error de validación: sacudida horizontal breve
    public static async Task Sacudir(VisualElement v)
    {
        if (ReducirMovimiento) return;
        uint paso = 50;
        await v.TranslateTo(-8, 0, paso, Easing.CubicOut);
        await v.TranslateTo(8, 0, paso, Easing.CubicOut);
        await v.TranslateTo(-5, 0, paso, Easing.CubicOut);
        await v.TranslateTo(0, 0, paso, Easing.CubicOut);
    }

    // Nuevo póster en la vista previa: aparece con fade y escala leve (0.94 -> 1)
    public static async Task CambioPoster(VisualElement v)
    {
        if (ReducirMovimiento) return;
        v.Opacity = 0.3;
        v.Scale = 0.94;
        await Task.WhenAll(
            v.FadeTo(1, Media, Easing.CubicOut),
            v.ScaleTo(1, Media, Easing.CubicOut));
    }
}
