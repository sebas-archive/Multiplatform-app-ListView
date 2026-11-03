using System.Diagnostics;
using CineList.Core.Firestore;
using CineList.Core.Formulario;
using CineList.Core.Models;
using CineList.Helpers;
using CineList.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineList.ViewModels;

// Formulario para agregar ("formulario") o editar ("formulario?id=...") una película.
// En edición trabaja con un clon: la instancia de PeliculasViewModel nunca se modifica,
// al guardar se reemplaza por una nueva con Upsert.
public partial class FormularioViewModel(
    PeliculasViewModel lista, IPeliculasRepository repositorio, PosterService posters)
    : ObservableObject, IQueryAttributable
{
    private static readonly string[] GenerosFijos =
    [
        "Acción", "Animación", "Ciencia ficción", "Comedia", "Documental",
        "Drama", "Fantasía", "Romance", "Suspenso", "Terror",
    ];

    private Pelicula? _original;
    private DatosFormulario _inicial = DatosFormulario.Vacio;
    private bool _cargado;
    private bool _saliendo;
    private bool _intentoGuardar;
    private bool _buscandoFoto; // cámara o selector abiertos: evita abrir dos a la vez

    [ObservableProperty] private string _titulo = "";
    [ObservableProperty] private string _director = "";
    [ObservableProperty] private string _anio = "";
    [ObservableProperty] private string _duracion = "";
    [ObservableProperty] private string _sinopsis = "";
    [ObservableProperty] private string? _genero;
    [ObservableProperty] private bool _vista;
    [ObservableProperty] private int _calificacion;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HayPoster))] private string? _posterBase64;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HayErrores))] private IReadOnlyList<string> _errores = [];
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TextoGuardar), nameof(Libre))] private bool _guardando;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Libre))] private bool _procesandoFoto;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TituloPagina))] private bool _esEdicion;
    // Película "de mentira" para la vista previa del póster (id vacío: no entra en la caché)
    [ObservableProperty] private Pelicula _vistaPrevia = new();

    // Lista fija; en edición se agrega el género guardado si no es uno de los fijos (p. ej. creado desde la web)
    public IReadOnlyList<string> Generos { get; private set; } = GenerosFijos;
    public string TituloPagina => EsEdicion ? "Editar película" : "Nueva película";
    public string TextoGuardar => Guardando ? "Guardando..." : "Guardar";
    public bool HayPoster => !string.IsNullOrEmpty(PosterBase64);
    public bool HayErrores => Errores.Count > 0;
    public bool Libre => !Guardando && !ProcesandoFoto;
    public bool PuedeTomarFoto => posters.PuedeTomarFoto;
    public bool SinCamara => !PuedeTomarFoto;
    public bool HayCambios => Datos() != _inicial;

    // Guardar encontró errores: la página sacude la caja de errores (no al revalidar mientras se escribe)
    public event Action? ValidacionFallida;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_cargado) return; // Shell puede volver a llamar al regresar a la página
        _cargado = true;
        if (!query.TryGetValue("id", out var valor)) return;

        var id = Uri.UnescapeDataString(valor?.ToString() ?? "");
        _original = lista.Buscar(id)?.Clonar();
        if (_original is null)
        {
            _ = SalirConAvisoAsync("Película no encontrada", "Esta película ya no existe");
            return;
        }
        EsEdicion = true;
        Cargar(DatosFormulario.Desde(_original));
    }

    private void Cargar(DatosFormulario d)
    {
        var genero = d.Genero.Trim();
        if (genero.Length > 0 && !GenerosFijos.Contains(genero))
        {
            Generos = [.. GenerosFijos, genero];
            OnPropertyChanged(nameof(Generos));
        }
        Titulo = d.Titulo;
        Director = d.Director;
        Anio = d.Anio;
        Duracion = d.Duracion;
        Sinopsis = d.Sinopsis;
        Genero = genero.Length > 0 ? genero : null;
        Vista = d.Vista;
        Calificacion = d.Calificacion;
        PosterBase64 = d.PosterBase64;
        _inicial = Datos(); // la referencia para "hay cambios" es lo que realmente muestra el formulario
        ActualizarVistaPrevia();
    }

    private DatosFormulario Datos() => new(
        Titulo ?? "", Director ?? "", Genero ?? "", Anio ?? "", Duracion ?? "", Sinopsis ?? "",
        Vista, Calificacion, PosterBase64);

    partial void OnTituloChanged(string value)
    {
        if (!HayPoster) ActualizarVistaPrevia(); // con foto, el título no se ve en el póster
        Revalidar();
    }

    partial void OnAnioChanged(string value) => Revalidar();
    partial void OnDuracionChanged(string value) => Revalidar();
    partial void OnPosterBase64Changed(string? value) => ActualizarVistaPrevia();

    private void ActualizarVistaPrevia() =>
        VistaPrevia = new Pelicula { Titulo = Titulo ?? "", PosterBase64 = PosterBase64 };

    // Tras un intento fallido, los errores se actualizan mientras el usuario corrige
    private void Revalidar()
    {
        if (!_intentoGuardar) return;
        Errores = ValidadorPelicula.Validar(Titulo, Anio, Duracion, DateTime.Now.Year);
    }

    [RelayCommand]
    private Task TomarFotoAsync() => ObtenerPosterAsync(posters.TomarFotoAsync);

    [RelayCommand]
    private Task ElegirFotoAsync() => ObtenerPosterAsync(posters.ElegirDeGaleriaAsync);

    [RelayCommand]
    private void QuitarFoto()
    {
        if (!Libre) return;
        PosterBase64 = null;
    }

    // El indicador sobre el póster aparece solo mientras se comprime (no durante la cámara o el selector)
    private async Task ObtenerPosterAsync(Func<Action?, Task<string?>> obtener)
    {
        if (!Libre || _buscandoFoto) return;
        _buscandoFoto = true;
        try
        {
            var base64 = await obtener(() => ProcesandoFoto = true);
            if (base64 is not null) PosterBase64 = base64;
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[FormularioViewModel] {ex}");
            await AlertaAsync("Foto no válida", ex.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FormularioViewModel] {ex}");
            await AlertaAsync("Foto no válida", "No se pudo procesar la imagen");
        }
        finally
        {
            ProcesandoFoto = false;
            _buscandoFoto = false;
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!Libre || _saliendo) return;
        _intentoGuardar = true;
        Errores = ValidadorPelicula.Validar(Titulo, Anio, Duracion, DateTime.Now.Year);
        if (HayErrores)
        {
            ValidacionFallida?.Invoke();
            return;
        }

        Guardando = true;
        try
        {
            var datos = Datos();
            if (EsEdicion && _original is not null)
            {
                var editada = datos.CrearPelicula(_original);
                await repositorio.ActualizarAsync(editada);
                ImagenPoster.Invalidar(editada.Id); // antes de Upsert: la celda no debe pintar el póster viejo
                lista.Upsert(editada);
                _original = editada;
            }
            else
            {
                var creada = await repositorio.CrearAsync(datos.CrearPelicula());
                ImagenPoster.Invalidar(creada.Id);
                lista.Upsert(creada);
                // Ya existe en Firestore: si cerrar el formulario falla, otro "Guardar" debe actualizar, no duplicar
                _original = creada;
                EsEdicion = true;
            }
            _inicial = datos;
        }
        catch (Exception ex)
        {
            // Sin red o Firestore caído: se avisa y el formulario conserva lo escrito
            Debug.WriteLine($"[FormularioViewModel] {ex}");
            await AlertaAsync("No se pudo guardar",
                MensajesError.Para(ex, "No se pudo guardar la película"));
            Guardando = false;
            return;
        }

        _saliendo = true;
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _saliendo = false;
            Guardando = false;
            Debug.WriteLine($"[FormularioViewModel] {ex}");
            await AlertaAsync("Error", "La película se guardó, pero no se pudo cerrar el formulario");
        }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        if (_saliendo || Guardando) return;
        _saliendo = true;
        try
        {
            if (HayCambios && Shell.Current is { } shell
                && !await shell.DisplayAlert("Descartar cambios", "¿Salir sin guardar?", "Descartar", "Seguir editando"))
            {
                _saliendo = false;
                return;
            }
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _saliendo = false;
            Debug.WriteLine($"[FormularioViewModel] {ex}");
            await AlertaAsync("Error", "No se pudo cerrar el formulario");
        }
    }

    private async Task SalirConAvisoAsync(string titulo, string mensaje)
    {
        _saliendo = true;
        try
        {
            // Se espera a que termine la navegación de entrada antes de mostrar la alerta
            await Task.Delay(400);
            await AlertaAsync(titulo, mensaje);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _saliendo = false;
            Debug.WriteLine($"[FormularioViewModel] {ex}");
        }
    }

    private static async Task AlertaAsync(string titulo, string mensaje)
    {
        try
        {
            if (Shell.Current is { } shell) await shell.DisplayAlert(titulo, mensaje, "Aceptar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FormularioViewModel] {ex.Message}");
        }
    }
}
