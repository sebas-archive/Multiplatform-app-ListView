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

    public PeliculasPage(PeliculasViewModel vm, TemaService tema)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _tema = tema;
        BotonTema.Text = IconoTema();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (!_vm.Cargada) _ = _vm.CargarCommand.ExecuteAsync(null);
            await Animaciones.AparecerEscala(BotonAgregar);
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

        try
        {
            if (PeliculaViewCell.VistaDe(pelicula) is { } celda)
                await Animaciones.Pulsar(celda);
            await Shell.Current.GoToAsync($"detalle?id={pelicula.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeliculasPage] Ruta de detalle no disponible: {ex.Message}");
        }
    }

    private async void OnAgregar(object? sender, EventArgs e)
    {
        try
        {
            await Animaciones.RotarFab(BotonAgregar);
            await Shell.Current.GoToAsync("formulario");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeliculasPage] Ruta de formulario no disponible: {ex.Message}");
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

    // En modo oscuro se ofrece el sol (pasar a claro); en modo claro, la luna
    private string IconoTema() => _tema.TemaActual == AppTheme.Dark ? Iconos.ModoClaro : Iconos.ModoOscuro;
}
