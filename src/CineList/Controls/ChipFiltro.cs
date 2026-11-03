using CineList.Core.Lista;
using CineList.Helpers;

namespace CineList.Controls;

// Chip de filtro (Todas / Vistas / Pendientes). Está activo cuando Seleccionado == Valor;
// al cambiar, el fondo pasa de un color a otro en 200 ms.
public class ChipFiltro : Button
{
    public static readonly BindableProperty ValorProperty = BindableProperty.Create(
        nameof(Valor), typeof(FiltroEstado), typeof(ChipFiltro), FiltroEstado.Todas,
        propertyChanged: (b, _, _) => ((ChipFiltro)b).Actualizar(animar: false));

    public static readonly BindableProperty SeleccionadoProperty = BindableProperty.Create(
        nameof(Seleccionado), typeof(FiltroEstado), typeof(ChipFiltro), FiltroEstado.Todas,
        propertyChanged: (b, _, _) => ((ChipFiltro)b).Actualizar(animar: true));

    public FiltroEstado Valor
    {
        get => (FiltroEstado)GetValue(ValorProperty);
        set => SetValue(ValorProperty, value);
    }

    public FiltroEstado Seleccionado
    {
        get => (FiltroEstado)GetValue(SeleccionadoProperty);
        set => SetValue(SeleccionadoProperty, value);
    }

    private bool? _activo;

    public ChipFiltro()
    {
        Actualizar(animar: false);
    }

    private void Actualizar(bool animar)
    {
        var activo = Valor == Seleccionado;
        if (_activo == activo) return;
        _activo = activo;

        var desde = BackgroundColor;
        Style = (Style)Application.Current!.Resources[activo ? "ChipActivo" : "Chip"];
        if (!animar || desde is null || Animaciones.ReducirMovimiento) return;

        // Animación de color: se pinta localmente y al terminar se devuelve el control al estilo
        // (así el AppThemeBinding del estilo sigue funcionando al cambiar de tema)
        var hasta = BackgroundColor;
        this.AbortAnimation("ColorChip");
        new Animation(t => BackgroundColor = Interpolar(desde, hasta, t), 0, 1, Easing.CubicOut)
            .Commit(this, "ColorChip", 16, 200, finished: (_, cancelada) =>
            {
                if (!cancelada) ClearValue(BackgroundColorProperty);
            });
    }

    private static Color Interpolar(Color a, Color b, double t) => new(
        (float)(a.Red + (b.Red - a.Red) * t),
        (float)(a.Green + (b.Green - a.Green) * t),
        (float)(a.Blue + (b.Blue - a.Blue) * t),
        (float)(a.Alpha + (b.Alpha - a.Alpha) * t));
}
