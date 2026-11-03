using CineList.Core.Models;
using CineList.Core.Poster;
using CineList.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CineList.Controls;

// Póster de una película: la foto guardada (base64) o, si no hay, un degradado con las iniciales del título.
public class PosterView : Border
{
    // Pares de colores (arriba, abajo) para el póster generado
    private static readonly (Color Inicio, Color Fin)[] Paleta =
    [
        (Color.FromArgb("#2B5876"), Color.FromArgb("#4E4376")),
        (Color.FromArgb("#C94B4B"), Color.FromArgb("#4B134F")),
        (Color.FromArgb("#3A7D44"), Color.FromArgb("#1D3B2A")),
        (Color.FromArgb("#D38312"), Color.FromArgb("#A83279")),
        (Color.FromArgb("#1F6F8B"), Color.FromArgb("#0B2A3A")),
        (Color.FromArgb("#614385"), Color.FromArgb("#516395")),
    ];

    public static readonly BindableProperty PeliculaProperty = BindableProperty.Create(
        nameof(Pelicula), typeof(Pelicula), typeof(PosterView),
        propertyChanged: (b, _, _) => ((PosterView)b).Actualizar());

    public Pelicula? Pelicula
    {
        get => (Pelicula?)GetValue(PeliculaProperty);
        set => SetValue(PeliculaProperty, value);
    }

    private readonly Image _imagen = new() { Aspect = Aspect.AspectFill };
    private readonly Label _iniciales = new()
    {
        FontFamily = "InterBold",
        TextColor = Colors.White,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    public PosterView()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 8 };
        Padding = 0;
        Content = new Grid { Children = { _iniciales, _imagen } };
        SizeChanged += (_, _) => _iniciales.FontSize = Math.Max(12, Width * 0.32);
    }

    // Vuelve a pintar el póster; llamar tras cambiar la foto de la misma película
    public void Actualizar()
    {
        var p = Pelicula;
        var foto = p is null ? null : ImagenPoster.Desde(p.PosterBase64, p.Id);
        _imagen.Source = foto;
        _imagen.IsVisible = foto is not null;

        var titulo = p?.Titulo ?? "";
        var (inicio, fin) = Paleta[PosterGenerado.IndicePaleta(titulo, Paleta.Length)];
        Background = new LinearGradientBrush(
            [new GradientStop(inicio, 0), new GradientStop(fin, 1)],
            new Point(0.2, 0), new Point(0.8, 1));
        _iniciales.Text = foto is null ? PosterGenerado.Iniciales(titulo) : "";
    }
}
