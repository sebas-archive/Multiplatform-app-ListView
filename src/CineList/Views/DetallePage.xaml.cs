using System.ComponentModel;
using System.Diagnostics;
using CineList.Helpers;
using CineList.ViewModels;

namespace CineList.Views;

public partial class DetallePage : ContentPage
{
    private readonly DetalleViewModel _vm;
    private bool _animada;
    private bool? _vistaMostrada;

    public DetallePage(DetalleViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
        PintarEtiqueta(_vm.Pelicula?.Vista ?? false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (!await _vm.RefrescarAsync()) return;
            PintarEtiqueta(_vm.Pelicula?.Vista ?? false);
            if (_animada) return; // al volver del formulario no se repite la entrada
            _animada = true;
            await Animaciones.EntradaDetalle(Poster, [BloqueTitulo, Meta, BloqueEstado, Sinopsis, Botones]);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DetallePage] {ex.Message}");
        }
        finally
        {
            // Pase lo que pase, el contenido queda visible
            if (_animada)
                foreach (var e in new VisualElement[] { Poster, BloqueTitulo, Meta, BloqueEstado, Sinopsis, Botones })
                {
                    e.Opacity = 1;
                    e.TranslationY = 0;
                    e.Scale = 1;
                }
        }
    }

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
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
