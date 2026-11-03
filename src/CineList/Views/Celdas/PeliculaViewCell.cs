using CineList.Controls;
using CineList.Core.Models;
using CineList.Helpers;
using CineList.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace CineList.Views.Celdas;

// Celda personalizada del ListView (Guía 8): póster, título, datos, estrellas y etiqueta VISTA/PENDIENTE.
// El ListView la usa con DataTemplate(typeof(PeliculaViewCell)); su BindingContext es una Pelicula.
public class PeliculaViewCell : ViewCell
{
    // Celdas creadas, para ubicar la vista de una película al tocar, eliminar o cambiar de estado.
    // El ListView también crea celdas solo para medir; esas no están cargadas en pantalla (IsLoaded).
    private static readonly List<WeakReference<PeliculaViewCell>> Celdas = [];
    private static bool _conectada;

    private readonly PeliculasViewModel _vm;
    private readonly PosterView _poster = new() { WidthRequest = 44, HeightRequest = 64 };
    private readonly Label _titulo = new() { FontFamily = "InterSemi", FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Label _datos = new() { FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly EstrellasView _estrellas = new() { Tamano = 13, EsEditable = false };
    private readonly Label _textoEtiqueta = new() { FontFamily = "InterBold", FontSize = 10, CharacterSpacing = 0.6 };
    private readonly Border _etiqueta;
    private readonly MenuItem _accionEstado;

    // Alto fijo de la fila (los encabezados de género miden 44)
    public const double AltoFila = 88;

    public PeliculaViewCell()
    {
        Height = AltoFila;
        _vm = IPlatformApplication.Current!.Services.GetRequiredService<PeliculasViewModel>();
        ConectarConViewModel(_vm);

        _datos.SetAppThemeColor(Label.TextColorProperty, Recurso("TextoSecundarioClaro"), Recurso("TextoSecundarioOscuro"));

        _etiqueta = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 3),
            VerticalOptions = LayoutOptions.Start,
            Content = _textoEtiqueta,
        };

        var textos = new VerticalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Children = { _titulo, _datos, _estrellas },
        };

        var fila = new Grid
        {
            ColumnDefinitions = [new(44), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 12,
            Children = { _poster },
        };
        fila.Add(textos, 1);
        fila.Add(_etiqueta, 2);

        var tarjeta = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(8),
            Content = fila,
        };
        tarjeta.SetAppThemeColor(VisualElement.BackgroundColorProperty, Recurso("SuperficieClaro"), Recurso("SuperficieOscuro"));
        tarjeta.SetAppTheme<Brush>(Border.StrokeProperty,
            new SolidColorBrush(Recurso("BordeSuperficieClaro")), new SolidColorBrush(Recurso("BordeSuperficieOscuro")));

        View = new ContentView { Padding = new Thickness(16, 4), Content = tarjeta };

        // Menú contextual (mantener presionado)
        _accionEstado = new MenuItem { Command = _vm.AlternarVistaCommand };
        _accionEstado.SetBinding(MenuItem.CommandParameterProperty, ".");
        var eliminar = new MenuItem { Text = "Eliminar", IsDestructive = true, Command = _vm.EliminarCommand };
        eliminar.SetBinding(MenuItem.CommandParameterProperty, ".");
        ContextActions.Add(_accionEstado);
        ContextActions.Add(eliminar);

        Celdas.Add(new WeakReference<PeliculaViewCell>(this));
    }

    private Pelicula? Pelicula => BindingContext as Pelicula;

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Restablecer();
        Mostrar();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Mostrar();
        if (!_vm.AnimarEntrada) return;

        // Entrada escalonada: 40 ms entre filas
        var indice = _vm.SiguienteIndiceEntrada();
        View.Opacity = 0;
        await Task.Delay(indice * 40);
        await Animaciones.EntradaEscalonada([View]);
    }

    // Pinta los datos de la película en los controles
    private void Mostrar()
    {
        if (Pelicula is not { } p) return;
        _poster.Pelicula = p;
        _poster.Actualizar();
        _titulo.Text = p.Titulo;
        _datos.Text = string.Join(" · ", new[]
        {
            p.Director.Trim(),
            p.Anio > 0 ? p.Anio.ToString() : "",
            p.DuracionMin > 0 ? $"{p.DuracionMin} min" : "",
        }.Where(parte => parte.Length > 0));
        _estrellas.Valor = p.Calificacion;
        MostrarEstado(p.Vista);
    }

    private void MostrarEstado(bool vista)
    {
        _textoEtiqueta.Text = vista ? "VISTA" : "PENDIENTE";
        _textoEtiqueta.SetAppThemeColor(Label.TextColorProperty,
            Recurso(vista ? "VistaTextoClaro" : "PendienteTextoClaro"),
            Recurso(vista ? "VistaTextoOscuro" : "PendienteTextoOscuro"));
        _etiqueta.SetAppThemeColor(VisualElement.BackgroundColorProperty,
            Recurso(vista ? "VistaFondoClaro" : "PendienteFondoClaro"),
            Recurso(vista ? "VistaFondoOscuro" : "PendienteFondoOscuro"));
        _accionEstado.Text = vista ? "Marcar pendiente" : "Marcar vista";
    }

    // Las celdas se reciclan: una animación de borrado no debe dejar la fila vacía
    private void Restablecer()
    {
        View.CancelAnimations();
        View.Opacity = 1;
        View.TranslationY = 0;
        View.Scale = 1;
        View.HeightRequest = -1;
    }

    // Vista raíz de la celda que muestra esta película (o null si no está en pantalla)
    public static View? VistaDe(Pelicula pelicula) => CeldaDe(pelicula)?.View;

    private static PeliculaViewCell? CeldaDe(Pelicula pelicula)
    {
        Celdas.RemoveAll(w => !w.TryGetTarget(out _));
        foreach (var w in Celdas)
            if (w.TryGetTarget(out var celda) && celda.View.IsLoaded && celda.Pelicula?.Id == pelicula.Id)
                return celda;
        return null;
    }

    // Se conecta una sola vez al ViewModel (singleton) con métodos estáticos: no retiene celdas
    private static void ConectarConViewModel(PeliculasViewModel vm)
    {
        if (_conectada) return;
        _conectada = true;
        vm.PeliculaActualizada += CambioDeEstado;
        vm.AnimarEliminacion = AnimarEliminacionAsync;
    }

    // Marcar vista/pendiente: la etiqueta cambia con fade + leve rebote
    private static async void CambioDeEstado(Pelicula pelicula)
    {
        if (CeldaDe(pelicula) is not { } celda) return;
        if (Animaciones.ReducirMovimiento)
        {
            celda.MostrarEstado(pelicula.Vista);
            return;
        }
        await celda._etiqueta.FadeTo(0, 120, Easing.CubicOut);
        celda.MostrarEstado(pelicula.Vista);
        await Task.WhenAll(celda._etiqueta.FadeTo(1, 150, Easing.CubicOut), Animaciones.Rebote(celda._etiqueta));
    }

    private static Task AnimarEliminacionAsync(Pelicula pelicula) =>
        VistaDe(pelicula) is { } vista ? Animaciones.DesvanecerYColapsar(vista) : Task.CompletedTask;

    private static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out var valor) == true && valor is Color c ? c : Colors.Gray;
}
