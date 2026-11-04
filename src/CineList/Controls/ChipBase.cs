using CineList.Helpers;

namespace CineList.Controls;

// Base de los chips: alterna entre los estilos "Chip" y "ChipActivo"; al cambiar,
// el fondo pasa de un color a otro en 200 ms.
public abstract class ChipBase : Button
{
    private bool? _activo;

    protected void Marcar(bool activo, bool animar)
    {
        if (_activo == activo) return;
        _activo = activo;

        // El color visible ahora (puede venir de una animación a medias); luego se quita cualquier
        // valor local para que el fondo vuelva a salir del estilo
        var desde = BackgroundColor;
        this.AbortAnimation("ColorChip");
        ClearValue(BackgroundColorProperty);
        Style = (Style)Application.Current!.Resources[activo ? "ChipActivo" : "Chip"];
        if (!animar || desde is null || Handler is null || Animaciones.ReducirMovimiento) return;

        // Animación de color: se pinta localmente y al terminar (o cancelarse) se devuelve el control
        // al estilo, así el AppThemeBinding del estilo sigue funcionando al cambiar de tema
        var hasta = BackgroundColor;
        new Animation(t => BackgroundColor = Interpolar(desde, hasta, t), 0, 1, Easing.CubicOut)
            .Commit(this, "ColorChip", 16, 200, finished: (_, _) => ClearValue(BackgroundColorProperty));
    }

    private static Color Interpolar(Color a, Color b, double t) => new(
        (float)(a.Red + (b.Red - a.Red) * t),
        (float)(a.Green + (b.Green - a.Green) * t),
        (float)(a.Blue + (b.Blue - a.Blue) * t),
        (float)(a.Alpha + (b.Alpha - a.Alpha) * t));
}
