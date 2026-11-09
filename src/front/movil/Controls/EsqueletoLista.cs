using CineList.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace CineList.Controls;

// Esqueleto (shimmer) de 5 filas mientras carga la lista: la opacidad pulsa en bucle
// mientras el control está visible y se detiene al ocultarse.
public class EsqueletoLista : VerticalStackLayout
{
    private const string NombreAnimacion = "PulsoEsqueleto";

    public EsqueletoLista()
    {
        Spacing = 0;
        for (var i = 0; i < 5; i++) Children.Add(CrearFila());
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName != nameof(IsVisible)) return;
        if (IsVisible) Iniciar();
        else this.AbortAnimation(NombreAnimacion);
    }

    private void Iniciar()
    {
        Opacity = 1;
        if (Animaciones.ReducirMovimiento) return;
        new Animation
        {
            { 0, 0.5, new Animation(v => Opacity = v, 1, 0.45, Easing.CubicOut) },
            { 0.5, 1, new Animation(v => Opacity = v, 0.45, 1, Easing.CubicOut) },
        }.Commit(this, NombreAnimacion, 16, 1100, repeat: () => IsVisible);
    }

    private static View CrearFila()
    {
        var poster = Caja(44, 64, 8);
        var barras = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children = { Caja(150, 12, 6), Caja(110, 10, 5), Caja(70, 10, 5) },
        };
        var fila = new Grid { ColumnDefinitions = [new(44), new(GridLength.Star)], ColumnSpacing = 12 };
        fila.Add(poster, 0);
        fila.Add(barras, 1);

        var tarjeta = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = 8,
            Content = fila,
        };
        tarjeta.SetAppThemeColor(VisualElement.BackgroundColorProperty, Recurso("SuperficieClaro"), Recurso("SuperficieOscuro"));
        return new ContentView { Padding = new Thickness(16, 4), HeightRequest = 88, Content = tarjeta };
    }

    // Bloque gris redondeado (marcador de posición)
    private static View Caja(double ancho, double alto, double radio)
    {
        var caja = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = radio },
            WidthRequest = ancho,
            HeightRequest = alto,
            HorizontalOptions = LayoutOptions.Start,
            Opacity = 0.22,
        };
        caja.SetAppThemeColor(VisualElement.BackgroundColorProperty, Recurso("TextoSecundarioClaro"), Recurso("TextoSecundarioOscuro"));
        return caja;
    }

    private static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out var valor) == true && valor is Color c ? c : Colors.Gray;
}
