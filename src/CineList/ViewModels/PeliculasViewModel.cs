using System.Collections.ObjectModel;
using System.Diagnostics;
using CineList.Core.Firestore;
using CineList.Core.Lista;
using CineList.Core.Models;
using CineList.Helpers;
using CineList.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineList.ViewModels;

// Estado de la lista de películas. Se registra como singleton: detalle y formulario
// usan Buscar/Upsert/Quitar para mantener la lista al día sin volver a descargarla.
public partial class PeliculasViewModel(IPeliculasRepository repositorio) : ObservableObject
{
    private const int MaxFilasAnimadas = 10;

    private List<Pelicula> _todas = [];
    private bool _cargada;
    private bool _ocupado;
    private int _siguienteIndiceEntrada;
    private CancellationTokenSource? _finEntrada;

    // Origen del ListView (agrupado por género)
    public ObservableCollection<GrupoPeliculas> Grupos { get; } = [];

    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private FiltroEstado _estado = FiltroEstado.Todas;
    [ObservableProperty] private bool _cargando;
    [ObservableProperty] private bool _refrescando;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string _iconoError = Iconos.Pelicula;
    [ObservableProperty] private string _resumen = "";
    [ObservableProperty] private bool _vacia;
    [ObservableProperty] private bool _sinResultados;
    [ObservableProperty] private bool _animarEntrada;

    public bool HayError => Error is not null;
    public bool MostrarLista => !HayError;
    public bool MostrarResumen => !Cargando && !Vacia && !HayError && !SinResultados;
    public bool Cargada => _cargada;

    // La página conecta aquí la animación de la celda que se elimina (fade + colapso)
    public Func<Pelicula, Task>? AnimarEliminacion { get; set; }

    // Se dispara cuando una película cambia sin salir de la lista (p. ej. vista/pendiente)
    public event Action<Pelicula>? PeliculaActualizada;

    partial void OnTextoBusquedaChanged(string value) => Aplicar(animar: false);
    partial void OnEstadoChanged(FiltroEstado value) => Aplicar(animar: true);
    partial void OnErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HayError));
        OnPropertyChanged(nameof(MostrarLista));
        OnPropertyChanged(nameof(MostrarResumen));
    }
    partial void OnCargandoChanged(bool value) => OnPropertyChanged(nameof(MostrarResumen));
    partial void OnVaciaChanged(bool value) => OnPropertyChanged(nameof(MostrarResumen));
    partial void OnSinResultadosChanged(bool value) => OnPropertyChanged(nameof(MostrarResumen));

    [RelayCommand]
    private Task CargarAsync() => DescargarAsync(mostrarEsqueleto: true);

    [RelayCommand]
    private Task RefrescarAsync() => DescargarAsync(mostrarEsqueleto: false);

    [RelayCommand]
    private void CambiarEstado(FiltroEstado estado) => Estado = estado;

    [RelayCommand]
    private async Task CargarEjemplosAsync()
    {
        if (_ocupado) return;
        _ocupado = true;
        Cargando = true;
        Vacia = false;
        try
        {
            await repositorio.CargarEjemplosAsync();
        }
        catch (Exception ex)
        {
            MostrarError(ex);
            Cargando = false;
            _ocupado = false;
            return;
        }
        _ocupado = false;
        await DescargarAsync(mostrarEsqueleto: true);
    }

    [RelayCommand]
    private async Task AlternarVistaAsync(Pelicula? pelicula)
    {
        if (pelicula is null) return;
        var copia = pelicula.Clonar();
        copia.Vista = !pelicula.Vista;
        try
        {
            await repositorio.ActualizarAsync(copia);
        }
        catch (Exception ex)
        {
            await AvisarAsync(ex);
            return;
        }
        pelicula.Vista = copia.Vista;
        if (FiltroPeliculas.Filtrar([pelicula], TextoBusqueda, Estado).Count == 0)
        {
            Aplicar(animar: false); // ya no cumple el filtro: sale de la lista
        }
        else
        {
            Resumen = FiltroPeliculas.Resumen(Grupos.SelectMany(g => g));
            PeliculaActualizada?.Invoke(pelicula);
        }
    }

    [RelayCommand]
    private async Task EliminarAsync(Pelicula? pelicula)
    {
        if (pelicula is null || Shell.Current is null) return;
        var confirmar = await Shell.Current.DisplayAlert(
            "Eliminar película", $"¿Eliminar \"{pelicula.Titulo}\"?", "Eliminar", "Cancelar");
        if (!confirmar) return;
        try
        {
            await repositorio.EliminarAsync(pelicula.Id);
        }
        catch (Exception ex)
        {
            await AvisarAsync(ex);
            return;
        }
        if (AnimarEliminacion is not null) await AnimarEliminacion(pelicula);
        ImagenPoster.Invalidar(pelicula.Id);
        Quitar(pelicula.Id);
    }

    public Pelicula? Buscar(string id) => _todas.FirstOrDefault(p => p.Id == id);

    // Agrega o reemplaza una película (tras crear o editar) y re-aplica el filtro
    public void Upsert(Pelicula pelicula)
    {
        var i = _todas.FindIndex(p => p.Id == pelicula.Id);
        if (i >= 0) _todas[i] = pelicula;
        else _todas.Insert(0, pelicula);
        ImagenPoster.Invalidar(pelicula.Id);
        Aplicar(animar: false);
    }

    public void Quitar(string id)
    {
        _todas.RemoveAll(p => p.Id == id);
        Aplicar(animar: false);
    }

    // La celda pide su posición en la entrada escalonada (máx. 10 filas con retraso)
    public int SiguienteIndiceEntrada() => Math.Min(_siguienteIndiceEntrada++, MaxFilasAnimadas);

    private async Task DescargarAsync(bool mostrarEsqueleto)
    {
        if (_ocupado)
        {
            Refrescando = false;
            return;
        }
        _ocupado = true;
        if (mostrarEsqueleto) Cargando = true;
        try
        {
            _todas = (await repositorio.ListarAsync()).ToList();
            _cargada = true;
            Error = null;
            Aplicar(animar: true);
        }
        catch (Exception ex)
        {
            MostrarError(ex);
        }
        finally
        {
            Cargando = false;
            Refrescando = false;
            _ocupado = false;
        }
    }

    // Filtra y agrupa en memoria (sin red)
    private void Aplicar(bool animar)
    {
        var filtradas = FiltroPeliculas.Filtrar(_todas, TextoBusqueda, Estado);
        if (animar) IniciarEntrada();

        Grupos.Clear();
        foreach (var grupo in FiltroPeliculas.Agrupar(filtradas))
            Grupos.Add(grupo);

        Resumen = FiltroPeliculas.Resumen(filtradas);
        Vacia = _cargada && Error is null && _todas.Count == 0;
        SinResultados = _todas.Count > 0 && filtradas.Count == 0;
    }

    // Activa la entrada escalonada y la apaga a los 600 ms para que el scroll no la repita
    private async void IniciarEntrada()
    {
        _finEntrada?.Cancel();
        var cts = _finEntrada = new CancellationTokenSource();
        _siguienteIndiceEntrada = 0;
        AnimarEntrada = true;
        try
        {
            await Task.Delay(600, cts.Token);
            AnimarEntrada = false;
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void MostrarError(Exception ex)
    {
        Debug.WriteLine($"[PeliculasViewModel] {ex}");
        if (ex is RepositorioException repo)
        {
            Error = repo.Message;
            IconoError = repo.SinConexion ? Iconos.SinConexion : Iconos.Pelicula;
        }
        else
        {
            Error = "No se pudo cargar las películas";
            IconoError = Iconos.Pelicula;
        }
        Vacia = false;
    }

    private static Task AvisarAsync(Exception ex)
    {
        Debug.WriteLine($"[PeliculasViewModel] {ex}");
        var mensaje = ex is RepositorioException ? ex.Message : "No se pudo completar la operación";
        return Shell.Current?.DisplayAlert("CineList", mensaje, "Aceptar") ?? Task.CompletedTask;
    }
}
