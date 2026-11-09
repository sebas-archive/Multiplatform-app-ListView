using System.Diagnostics;
using CineList.Core.Firestore;
using CineList.Core.Lista;
using CineList.Core.Models;
using CineList.Helpers;
using CineList.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineList.ViewModels;

// Detalle de una película. Recibe el id por la ruta "detalle?id=...".
// Nunca modifica la instancia que vive en PeliculasViewModel: trabaja con clones.
public partial class DetalleViewModel(PeliculasViewModel lista, IPeliculasRepository repositorio)
    : ObservableObject, IQueryAttributable
{
    private bool _saliendo;

    [ObservableProperty] private Pelicula? _pelicula;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NoOcupado))] private bool _ocupado;

    // Se dispara solo tras guardar con éxito el cambio de estado (true = ahora está vista)
    public event Action<bool>? VistaAlternada;

    public string Id { get; private set; } = "";
    public bool NoOcupado => !Ocupado;

    public string Genero => (Pelicula?.Genero.Trim() is { Length: > 0 } g ? g : "Sin género").ToUpperInvariant();
    public string Meta => Pelicula is null ? "" : TextoPelicula.Meta(Pelicula);
    public bool HayMeta => Meta.Length > 0;
    public string Sinopsis => Pelicula?.Sinopsis.Trim() is { Length: > 0 } s ? s : "Esta película aún no tiene sinopsis.";
    public bool HaySinopsis => Pelicula?.Sinopsis.Trim().Length > 0;
    public string TextoEtiqueta => TextoPelicula.Etiqueta(Pelicula?.Vista ?? false);
    public string TextoBotonVista => Pelicula?.Vista == true ? "Marcar pendiente" : "Marcar vista";

    partial void OnPeliculaChanged(Pelicula? value)
    {
        OnPropertyChanged(nameof(Genero));
        OnPropertyChanged(nameof(Meta));
        OnPropertyChanged(nameof(HayMeta));
        OnPropertyChanged(nameof(Sinopsis));
        OnPropertyChanged(nameof(HaySinopsis));
        OnPropertyChanged(nameof(TextoEtiqueta));
        OnPropertyChanged(nameof(TextoBotonVista));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            Id = Uri.UnescapeDataString(id?.ToString() ?? "");
        Pelicula = lista.Buscar(Id);
    }

    // Se llama cada vez que la página aparece (también al volver del formulario).
    // Devuelve false si la película ya no existe.
    public async Task<bool> RefrescarAsync()
    {
        if (_saliendo) return true;
        var actual = lista.Buscar(Id);
        if (actual is null)
        {
            _saliendo = true;
            try
            {
                if (Shell.Current is not { } shell)
                {
                    _saliendo = false;
                    return false;
                }
                await shell.DisplayAlert("Película no encontrada", "Esta película ya no existe", "Aceptar");
                await shell.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _saliendo = false;
                Debug.WriteLine($"[DetalleViewModel] {ex}");
            }
            return false;
        }
        if (!ReferenceEquals(actual, Pelicula)) Pelicula = actual;
        return true;
    }

    [RelayCommand]
    private async Task AlternarVistaAsync()
    {
        if (Ocupado || Pelicula is null) return;
        Ocupado = true;
        try
        {
            var copia = Pelicula.Clonar();
            copia.Vista = !copia.Vista;
            await repositorio.ActualizarAsync(copia);
            lista.Upsert(copia);
            Pelicula = copia;
            VistaAlternada?.Invoke(copia.Vista);
        }
        catch (Exception ex)
        {
            await AvisarAsync(ex);
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (Ocupado || Pelicula is null) return;
        Ocupado = true;
        try
        {
            await Shell.Current.GoToAsync($"formulario?id={Uri.EscapeDataString(Id)}");
        }
        catch (Exception ex)
        {
            await AvisarAsync(ex);
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (Ocupado || Pelicula is not { } pelicula || Shell.Current is null) return;
        Ocupado = true;
        try
        {
            var confirmar = await Shell.Current.DisplayAlert(
                "Eliminar película", $"¿Eliminar \"{pelicula.Titulo}\"?", "Eliminar", "Cancelar");
            if (!confirmar) return;
            await repositorio.EliminarAsync(pelicula.Id);
            ImagenPoster.Invalidar(pelicula.Id);
            lista.Quitar(pelicula.Id);
            _saliendo = true;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _saliendo = false;
            await AvisarAsync(ex);
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    private async Task VolverAsync()
    {
        if (_saliendo) return; // evita un segundo "atrás" encolado por doble toque
        _saliendo = true;
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _saliendo = false;
            Debug.WriteLine($"[DetalleViewModel] {ex.Message}");
        }
    }

    private static async Task AvisarAsync(Exception ex)
    {
        Debug.WriteLine($"[DetalleViewModel] {ex}");
        var mensaje = MensajesError.Para(ex, "No se pudo completar la acción");
        try
        {
            if (Shell.Current is { } shell) await shell.DisplayAlert("Error", mensaje, "Aceptar");
        }
        catch (Exception ex2)
        {
            Debug.WriteLine($"[DetalleViewModel] {ex2.Message}");
        }
    }
}
