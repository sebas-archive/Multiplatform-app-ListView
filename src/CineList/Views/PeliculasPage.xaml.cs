using System.ComponentModel;
using System.Diagnostics;
using CineList.Core.Models;
using CineList.Helpers;
using CineList.Services;
using CineList.ViewModels;
using CineList.Views.Celdas;

namespace CineList.Views;

public partial class PeliculasPage : ContentPage
{
    private readonly PeliculasViewModel _vm;
    private readonly TemaService _tema;
    private bool _navegando; // evita abrir dos páginas con un doble toque

    public PeliculasPage(PeliculasViewModel vm, TemaService tema)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _tema = tema;
        BotonTema.Text = IconoTema();
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    // Insignia del botón Filtros: aparece con escala al pasar de 0 a 1 o más criterios;
    // la línea "Activos" entra deslizando 12 px con fade
    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PeliculasViewModel.HayFiltrosActivos)) return;
        try
        {
            if (_vm.HayFiltrosActivos)
            {
                await Task.WhenAll(AparecerLineaActivos(), Animaciones.AparecerEscala(InsigniaFiltros));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeliculasPage] {ex.Message}");
        }
        finally
        {
            InsigniaFiltros.Scale = InsigniaFiltros.Opacity = 1;
            LineaActivos.Opacity = 1;
            LineaActivos.TranslationY = 0;
        }
    }

    private async Task AparecerLineaActivos()
    {
        if (Animaciones.ReducirMovimiento) return;
        LineaActivos.Opacity = 0;
        LineaActivos.TranslationY = -12;
        await Task.WhenAll(
            LineaActivos.FadeTo(1, 250, Easing.CubicOut),
            LineaActivos.TranslateTo(0, 0, 250, Easing.CubicOut));
    }

    private void OnFiltros(object? sender, TappedEventArgs e) => AbrirPanelFiltros();

    // Task 3 abre aquí el panel de filtros y orden
    private void AbrirPanelFiltros()
    {
    }

    private bool _fabAnimado;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (!_vm.Cargada) _ = _vm.CargarCommand.ExecuteAsync(null);
            // El "+" quedó girado 90° al abrir el formulario: con la sombra de Android, la rotación
            // desplaza el botón; se vuelve a 0 (el glifo se ve igual)
            BotonAgregar.Rotation = 0;
            // La animación de entrada solo corre la primera vez: al volver de segundo plano Android
            // pausa los animadores y el botón podía quedar a medio escalar
            if (_fabAnimado) BotonAgregar.Scale = BotonAgregar.Opacity = 1;
            else
            {
                _fabAnimado = true;
                await Animaciones.AparecerEscala(BotonAgregar);
                BotonAgregar.Scale = BotonAgregar.Opacity = 1;
            }
        }
        catch (Exception ex)
        {
            BotonAgregar.Scale = BotonAgregar.Opacity = 1;
            Debug.WriteLine($"[PeliculasPage] {ex.Message}");
        }
    }

    // Evento de selección de registro (Guía 8)
    private async void OnPeliculaTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is not Pelicula pelicula) return;
        ((ListView)sender).SelectedItem = null;
        if (_navegando) return;
        _navegando = true;

        try
        {
            if (PeliculaViewCell.VistaDe(pelicula) is { } celda)
                await Animaciones.Pulsar(celda);
            await Shell.Current.GoToAsync($"detalle?id={Uri.EscapeDataString(pelicula.Id)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeliculasPage] {ex.Message}");
            await AvisarNavegacionAsync("No se pudo abrir el detalle");
        }
        finally
        {
            _navegando = false;
        }
    }

    private async void OnAgregar(object? sender, EventArgs e)
    {
        if (_navegando) return;
        _navegando = true;
        try
        {
            await Animaciones.RotarFab(BotonAgregar);
            await Shell.Current.GoToAsync("formulario");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeliculasPage] {ex.Message}");
            await AvisarNavegacionAsync("No se pudo abrir el formulario");
        }
        finally
        {
            _navegando = false;
        }
    }

    private async void OnAlternarTema(object? sender, EventArgs e)
    {
        BotonTema.IsEnabled = false; // evita cambios encadenados mientras dura la animación
        try
        {
            var giro = Animaciones.ReducirMovimiento
                ? Task.CompletedTask
                : BotonTema.RotateTo(BotonTema.Rotation + 180, 350, Easing.CubicOut);
            await _tema.AlternarAsync(Contenido);
            await giro;
        }
        catch (Exception ex)
        {
            Contenido.Opacity = 1;
            Debug.WriteLine($"[PeliculasPage] {ex.Message}");
        }
        finally
        {
            BotonTema.Text = IconoTema();
            BotonTema.Rotation %= 360;
            BotonTema.IsEnabled = true;
        }
    }

    // Un fallo de navegación no debe pasar en silencio
    private static async Task AvisarNavegacionAsync(string mensaje)
    {
        try
        {
            if (Shell.Current is { } shell) await shell.DisplayAlert("Error", mensaje, "Aceptar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeliculasPage] {ex.Message}");
        }
    }

    // En modo oscuro se ofrece el sol (pasar a claro); en modo claro, la luna
    private string IconoTema() => _tema.TemaActual == AppTheme.Dark ? Iconos.ModoClaro : Iconos.ModoOscuro;
}
