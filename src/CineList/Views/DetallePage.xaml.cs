using System.ComponentModel;
using System.Diagnostics;
using CineList.Controls;
using CineList.Helpers;
using CineList.Services;
using CineList.ViewModels;

namespace CineList.Views;

public partial class DetallePage : ContentPage
{
    private readonly DetalleViewModel _vm;
    private bool _animada;
    private bool _visible;
    private bool? _vistaMostrada;

    public DetallePage(DetalleViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
        PintarEtiqueta(_vm.Pelicula?.Vista ?? false);
        PintarFondo();
        // El desenfoque se aplica a la vista nativa en cuanto existe (Android 12+)
        FondoArte.HandlerChanged += (_, _) => Desenfoque.Aplicar(FondoArte, 24);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        if (Application.Current is { } app)
        {
            app.RequestedThemeChanged -= OnTemaCambiado;
            app.RequestedThemeChanged += OnTemaCambiado;
        }
        PintarVelo();
        try
        {
            if (!await _vm.RefrescarAsync())
            {
                _animada = true; // si no se pudo salir, el contenido no queda invisible
                return;
            }
            PintarEtiqueta(_vm.Pelicula?.Vista ?? false);
            if (_animada) return; // al volver del formulario no se repite la entrada
            _animada = true;
            await EntradaAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DetallePage] {ex.Message}");
        }
        finally
        {
            // Pase lo que pase, el contenido queda visible
            if (_animada) MostrarTodo();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _visible = false;
        CortarVuelo();
        if (Application.Current is { } app) app.RequestedThemeChanged -= OnTemaCambiado;
    }

    // Atrás durante el vuelo: el póster superpuesto desaparece en el acto
    protected override bool OnBackButtonPressed()
    {
        CortarVuelo();
        return base.OnBackButtonPressed();
    }

    // Entrada: si se llegó tocando una fila, el póster vuela desde ella mientras el resto aparece
    // escalonado; luego las estrellas se llenan una a una y la etiqueta hace "pop"
    private async Task EntradaAsync()
    {
        var resto = new VisualElement[] { BloqueTitulo, Meta, BloqueEstado, Sinopsis, Botones };
        var hayOrigen = TransicionPoster.Tomar(_vm.Id, out var origen); // se consume siempre
        if (Animaciones.ReducirMovimiento)
        {
            await Animaciones.EntradaDetalle(Poster, resto);
            return;
        }
        Estrellas.PrepararLlenado();
        Etiqueta.Scale = 0;
        var fondo = Fondo.FadeTo(1, 350, Easing.CubicOut);
        if (hayOrigen)
            await Task.WhenAll(VolarPosterAsync(origen), Animaciones.EntradaEscalonada(resto, 50), fondo);
        else
            await Task.WhenAll(Animaciones.EntradaDetalle(Poster, resto), fondo);
        await Estrellas.AnimarLlenado();
        await Animaciones.PopEntrada(Etiqueta);
    }

    private async Task VolarPosterAsync(Rect origenEnVentana)
    {
        Poster.Opacity = 0;
        if (await DestinoVueloAsync() is not { } capa || Coordenadas.EnVentana(Poster) is not { } real)
        {
            // Sin layout a tiempo: entrada normal del póster
            await Animaciones.EntradaDetalle(Poster, []);
            return;
        }
        var destino = real.Offset(-capa.X, -capa.Y);
        var origen = origenEnVentana.Offset(-capa.X, -capa.Y);
        try
        {
            AbsoluteLayout.SetLayoutBounds(PosterVuelo, destino);
            var vuelo = Animaciones.VolarPoster(PosterVuelo, origen, destino);
            PosterVuelo.IsVisible = true;
            await vuelo;
        }
        finally
        {
            PosterVuelo.IsVisible = false;
            Poster.Opacity = 1;
        }
    }

    // Espera (máx. 300 ms) a que el póster real tenga tamaño y devuelve la capa del vuelo en
    // coordenadas de ventana; null si no hubo layout a tiempo o la página ya no está visible
    private async Task<Rect?> DestinoVueloAsync()
    {
        if (Poster.Width <= 0 || Poster.Height <= 0)
        {
            var medido = new TaskCompletionSource();
            void AlMedir(object? s, EventArgs e)
            {
                if (Poster.Width > 0 && Poster.Height > 0) medido.TrySetResult();
            }
            Poster.SizeChanged += AlMedir;
            try
            {
                await Task.WhenAny(medido.Task, Task.Delay(300));
            }
            finally
            {
                Poster.SizeChanged -= AlMedir;
            }
            if (!medido.Task.IsCompleted) return null;
        }
        // Un ciclo más para que termine el layout nativo de toda la página antes de medir posiciones
        await Dispatcher.DispatchAsync(() => { });
        return !_visible ? null : Coordenadas.EnVentana(CapaVuelo);
    }

    private void CortarVuelo()
    {
        Animaciones.CancelarVuelo(PosterVuelo);
        PosterVuelo.IsVisible = false;
        if (_animada) Poster.Opacity = 1;
    }

    private void MostrarTodo()
    {
        CortarVuelo();
        foreach (var e in new VisualElement[] { Poster, BloqueTitulo, Meta, BloqueEstado, Sinopsis, Botones, Fondo })
        {
            e.Opacity = 1;
            e.TranslationY = 0;
            e.Scale = 1;
        }
        Etiqueta.Scale = 1;
        Estrellas.MostrarLlenas();
        AplicarParallax(Desplazamiento.ScrollY);
    }

    // Parallax: el póster va a la mitad de velocidad y se encoge un poco; el fondo, más lento aún
    private void OnDesplazado(object? sender, ScrolledEventArgs e) => AplicarParallax(e.ScrollY);

    private void AplicarParallax(double y)
    {
        if (Animaciones.ReducirMovimiento) return;
        y = Math.Max(0, y); // el rebote de overscroll no mueve nada
        Poster.TranslationY = y * 0.5;
        Poster.Scale = 1 - Math.Min(y / 1200, 0.08);
        // El fondo está fuera del ScrollView: subir 0.3·y es ir más lento que el contenido
        Fondo.TranslationY = -y * 0.3;
    }

    // Fondo: la foto del póster o el degradado del póster generado
    private void PintarFondo()
    {
        var p = _vm.Pelicula;
        FondoArte.Background = PosterView.Degradado(p?.Titulo ?? "");
        var foto = p is null ? null : ImagenPoster.Desde(p.PosterBase64, p.Id);
        FondoImagen.Source = foto;
        FondoImagen.IsVisible = foto is not null;
    }

    // Velo del fondo: transparente arriba, color Fondo del tema abajo
    private void PintarVelo()
    {
        var oscuro = (Application.Current?.RequestedTheme ?? AppTheme.Dark) == AppTheme.Dark;
        var fondo = Recurso(oscuro ? "FondoOscuro" : "FondoClaro");
        FondoVelo.Background = new LinearGradientBrush(
            [new GradientStop(fondo.WithAlpha(0), 0), new GradientStop(fondo.WithAlpha(0.55f), 0.55f), new GradientStop(fondo, 1)],
            new Point(0, 0), new Point(0, 1));
    }

    private void OnTemaCambiado(object? sender, AppThemeChangedEventArgs e) => PintarVelo();

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DetalleViewModel.Pelicula)) PintarFondo();
        if (e.PropertyName != nameof(DetalleViewModel.TextoEtiqueta)) return;
        try
        {
            var vista = _vm.Pelicula?.Vista ?? false;
            if (_vistaMostrada == vista) return;
            // Cambio de estado visible: fade + leve rebote (como en la lista)
            if (_vistaMostrada is null || !_animada || Animaciones.ReducirMovimiento)
            {
                PintarEtiqueta(vista);
                return;
            }
            await Etiqueta.FadeTo(0, 120, Easing.CubicOut);
            PintarEtiqueta(vista);
            await Task.WhenAll(Etiqueta.FadeTo(1, 150, Easing.CubicOut), Animaciones.Rebote(Etiqueta));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DetallePage] {ex.Message}");
        }
        finally
        {
            Etiqueta.Opacity = 1;
            Etiqueta.Scale = 1;
        }
    }

    private void PintarEtiqueta(bool vista)
    {
        _vistaMostrada = vista;
        TextoEtiqueta.SetAppThemeColor(Label.TextColorProperty,
            Recurso(vista ? "VistaTextoClaro" : "PendienteTextoClaro"),
            Recurso(vista ? "VistaTextoOscuro" : "PendienteTextoOscuro"));
        Etiqueta.SetAppThemeColor(VisualElement.BackgroundColorProperty,
            Recurso(vista ? "VistaFondoClaro" : "PendienteFondoClaro"),
            Recurso(vista ? "VistaFondoOscuro" : "PendienteFondoOscuro"));
    }

    private static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out var valor) == true && valor is Color c ? c : Colors.Gray;
}
