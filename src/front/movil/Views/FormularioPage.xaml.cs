using System.ComponentModel;
using System.Diagnostics;
using CineList.Helpers;
using CineList.Services;
using CineList.ViewModels;

namespace CineList.Views;

public partial class FormularioPage : ContentPage
{
    private readonly FormularioViewModel _vm;
    private bool _animada;

    public FormularioPage(FormularioViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
        _vm.ValidacionFallida += OnValidacionFallida;

        // Con "reducir movimiento" el modal se abre sin la animación del sistema
        if (Animaciones.ReducirMovimiento)
            Shell.SetPresentationMode(this, PresentationMode.Modal);

        // El borde del campo enfocado toma el color de acento
        ConectarFoco(CajaTitulo, CampoTitulo);
        ConectarFoco(CajaDirector, CampoDirector);
        ConectarFoco(CajaAnio, CampoAnio);
        ConectarFoco(CajaDuracion, CampoDuracion);
        ConectarFoco(CajaGenero, CampoGenero);
        ConectarFoco(CajaSinopsis, CampoSinopsis);

        // "Siguiente" del teclado pasa al próximo campo
        CampoTitulo.Completed += (_, _) => CampoDirector.Focus();
        CampoDirector.Completed += (_, _) => CampoAnio.Focus();
        CampoAnio.Completed += (_, _) => CampoDuracion.Focus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_animada) return;
        _animada = true;
        try
        {
            await Animaciones.EntradaModal(Contenido,
                [BloquePoster, BloqueTitulo, BloqueDirector, BloqueNumeros, BloqueGenero, BloqueSinopsis, BloqueEstado]);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FormularioPage] {ex.Message}");
        }
        finally
        {
            // Pase lo que pase, el formulario queda visible
            Contenido.Opacity = 1;
            Contenido.TranslationY = 0;
            foreach (var e in new VisualElement[] { BloquePoster, BloqueTitulo, BloqueDirector, BloqueNumeros, BloqueGenero, BloqueSinopsis, BloqueEstado })
            {
                e.Opacity = 1;
                e.TranslationY = 0;
            }
        }
    }

#if ANDROID
    // El modal vive en un diálogo con su propia ventana: al adjuntarse se pinta su barra de estado
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is Android.Views.View vista)
            vista.ViewAttachedToWindow += (_, _) =>
                BarraEstado.Aplicar(Application.Current?.RequestedTheme ?? AppTheme.Light);
    }
#endif

    // Botón "atrás" de Android: mismo camino que la X (pregunta si hay cambios sin guardar)
    protected override bool OnBackButtonPressed()
    {
        _vm.CancelarCommand.Execute(null);
        return true;
    }

    private async void OnValidacionFallida()
    {
        try
        {
            await Animaciones.Sacudir(CajaErrores);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FormularioPage] {ex.Message}");
        }
        finally
        {
            CajaErrores.TranslationX = 0;
        }
    }

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FormularioViewModel.PosterBase64) && _animada)
        {
            try
            {
                await Animaciones.CambioPoster(Poster);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FormularioPage] {ex.Message}");
            }
            finally
            {
                Poster.Opacity = 1;
                Poster.Scale = 1;
            }
        }
    }

    // Border.Stroke es un Brush: se asigna un pincel por tema
    private static void ConectarFoco(Border caja, View campo)
    {
        campo.Focused += (_, _) => PintarBorde(caja, "AcentoClaro", "AcentoOscuro");
        campo.Unfocused += (_, _) => PintarBorde(caja, "BordeCampoClaro", "BordeCampoOscuro");
    }

    private static void PintarBorde(Border caja, string claro, string oscuro) =>
        caja.SetAppTheme<Brush>(Border.StrokeProperty,
            new SolidColorBrush(Recurso(claro)), new SolidColorBrush(Recurso(oscuro)));

    private static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out var valor) == true && valor is Color c ? c : Colors.Gray;
}
