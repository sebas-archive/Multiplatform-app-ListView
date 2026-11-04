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
public partial class PeliculasViewModel(IPeliculasRepository repositorio, FiltrosGuardados guardados) : ObservableObject
{
    private const int MaxFilasAnimadas = 10;

    private List<Pelicula> _todas = [];
    private bool _cargada;
    private bool _ocupado;
    private int _siguienteIndiceEntrada;
    private CancellationTokenSource? _finEntrada;
    private CancellationTokenSource? _finAviso;

    // Origen del ListView (agrupado por género)
    public ObservableCollection<GrupoPeliculas> Grupos { get; } = [];

    // Criterios vigentes (filtros y orden guardados + texto y estado). Texto y Estado se sincronizan
    // desde TextoBusqueda y Estado, que son los que enlaza la página.
    private CriteriosFiltro _criterios = guardados.Cargar();

    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private FiltroEstado _estado = guardados.Cargar().Estado;
    [ObservableProperty] private bool _cargando;
    [ObservableProperty] private bool _refrescando;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string _iconoError = Iconos.Pelicula;
    [ObservableProperty] private string _resumen = "";
    [ObservableProperty] private bool _vacia;
    [ObservableProperty] private bool _sinResultados;
    [ObservableProperty] private bool _animarEntrada;
    // Mensaje breve no bloqueante (p. ej. falló el refresco pero la lista sigue visible)
    [ObservableProperty] private string? _aviso;

    public CriteriosFiltro Criterios => _criterios;
    public bool Agrupado => _criterios.Orden == OrdenPeliculas.Genero;
    public int FiltrosActivos => _criterios.CuantosActivos;
    public string TextoActivos => FiltroPeliculas.TextoActivos(_criterios);
    public bool HayFiltrosActivos => FiltrosActivos > 0;
    public bool SinResultadosConFiltros => SinResultados && HayFiltrosActivos;
    public bool SinResultadosSimple => SinResultados && !HayFiltrosActivos;
    // Solo lectura, para el panel de filtros
    public IReadOnlyList<Pelicula> Todas => _todas;

    public bool HayError => Error is not null;
    public bool MostrarLista => !HayError;
    public bool MostrarResumen => !Cargando && !Vacia && !HayError && !SinResultados;
    public bool HayAviso => Aviso is not null;
    public bool Cargada => _cargada;

    // La página conecta aquí la animación de la celda que se elimina (fade + colapso)
    public Func<Pelicula, Task>? AnimarEliminacion { get; set; }

    // Se dispara cuando una película cambia sin salir de la lista (p. ej. vista/pendiente)
    public event Action<Pelicula>? PeliculaActualizada;

    // Se dispara cuando una película sale de la lista (las celdas recicladas se restablecen)
    public event Action<Pelicula>? PeliculaQuitada;

    partial void OnTextoBusquedaChanged(string value)
    {
        _criterios = _criterios with { Texto = value };
        Aplicar(animar: false);
    }

    private bool _sincronizando;

    partial void OnEstadoChanged(FiltroEstado value)
    {
        if (_sincronizando) return;
        _criterios = _criterios with { Estado = value };
        guardados.Guardar(_criterios);
        Aplicar(animar: true);
    }

    partial void OnErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HayError));
        OnPropertyChanged(nameof(MostrarLista));
        OnPropertyChanged(nameof(MostrarResumen));
    }
    partial void OnAvisoChanged(string? value) => OnPropertyChanged(nameof(HayAviso));
    partial void OnCargandoChanged(bool value) => OnPropertyChanged(nameof(MostrarResumen));
    partial void OnVaciaChanged(bool value) => OnPropertyChanged(nameof(MostrarResumen));
    partial void OnSinResultadosChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarResumen));
        OnPropertyChanged(nameof(SinResultadosConFiltros));
        OnPropertyChanged(nameof(SinResultadosSimple));
    }

    // Aplica criterios nuevos desde el panel: guarda, sincroniza estado y re-filtra con entrada escalonada
    public void AplicarCriterios(CriteriosFiltro nuevos)
    {
        nuevos = nuevos with { Texto = TextoBusqueda };
        guardados.Guardar(nuevos);
        _criterios = nuevos;
        _sincronizando = true; // evita que OnEstadoChanged re-aplique y reemplace los criterios
        Estado = nuevos.Estado;
        _sincronizando = false;
        Aplicar(animar: true);
    }

    [RelayCommand]
    private void LimpiarFiltros() =>
        AplicarCriterios(CriteriosFiltro.Vacio with { Texto = TextoBusqueda });

    private void NotificarCriterios()
    {
        OnPropertyChanged(nameof(Criterios));
        OnPropertyChanged(nameof(Agrupado));
        OnPropertyChanged(nameof(FiltrosActivos));
        OnPropertyChanged(nameof(TextoActivos));
        OnPropertyChanged(nameof(HayFiltrosActivos));
        OnPropertyChanged(nameof(SinResultadosConFiltros));
        OnPropertyChanged(nameof(SinResultadosSimple));
    }

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
        // Excepción intencional a la regla de clonar: se muta la instancia en su lugar. Las filas
        // visibles se repintan con PeliculaActualizada; las que están fuera de pantalla se repintan
        // al reciclarse (rebind); ninguna otra pantalla viva conserva esta instancia.
        pelicula.Vista = copia.Vista;
        if (FiltroPeliculas.Contar([pelicula], _criterios) == 0)
        {
            QuitarDeGrupos(pelicula); // ya no cumple el filtro: sale de la lista
        }
        else
        {
            PeliculaActualizada?.Invoke(pelicula);
        }
        ActualizarConteos();
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
        PeliculaQuitada?.Invoke(pelicula);
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

    // Quita en su lugar (sin reconstruir la lista, para conservar la posición del scroll)
    public void Quitar(string id)
    {
        var pelicula = Buscar(id);
        _todas.RemoveAll(p => p.Id == id);
        if (pelicula is not null) QuitarDeGrupos(pelicula);
        ActualizarConteos();
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
        if (mostrarEsqueleto)
        {
            Error = null; // al reintentar se oculta el error y se ve el esqueleto
            Cargando = true;
        }
        // Con esqueleto, se muestra al menos 600 ms: un reintento sin red falla al instante
        // y sin esta pausa el usuario no notaría que se volvió a intentar
        var pausaMinima = mostrarEsqueleto ? Task.Delay(600) : Task.CompletedTask;
        try
        {
            var lista = await repositorio.ListarAsync();
            await pausaMinima;
            _todas = lista.ToList();
            _cargada = true;
            Error = null;
            Aplicar(animar: true);
        }
        catch (Exception ex) when (!mostrarEsqueleto && _todas.Count > 0)
        {
            // Falló el refresco con la lista ya cargada: se conserva y se avisa sin bloquear
            Debug.WriteLine($"[PeliculasViewModel] {ex}");
            MostrarAviso(MensajesError.Para(ex, "No se pudo actualizar la lista"));
        }
        catch (Exception ex)
        {
            await pausaMinima;
            MostrarError(ex);
        }
        finally
        {
            Cargando = false;
            Refrescando = false;
            _ocupado = false;
        }
    }

    // Filtra, ordena y agrupa en memoria (sin red). Con orden distinto de Género el resultado es un
    // único grupo de género "" (lista plana); un grupo vacío no se agrega para que 0 resultados
    // sea igual en ambos modos.
    private void Aplicar(bool animar)
    {
        var resultado = FiltroPeliculas.Aplicar(_todas, _criterios);
        if (animar) IniciarEntrada();

        Grupos.Clear();
        foreach (var grupo in resultado.Grupos)
            if (grupo.Count > 0) Grupos.Add(grupo);
        NotificarCriterios();
        ActualizarConteos();
    }

    // Saca una película de su grupo; si el grupo queda vacío, se quita el grupo
    private void QuitarDeGrupos(Pelicula pelicula)
    {
        for (var i = 0; i < Grupos.Count; i++)
        {
            var grupo = Grupos[i];
            if (!grupo.Any(p => p.Id == pelicula.Id)) continue;
            if (grupo.Count == 1) Grupos.RemoveAt(i);
            else Grupos[i] = new GrupoPeliculas(grupo.Genero, grupo.Where(p => p.Id != pelicula.Id));
            return;
        }
    }

    private void ActualizarConteos()
    {
        var visibles = Grupos.SelectMany(g => g).ToList();
        Resumen = FiltroPeliculas.Resumen(visibles);
        Vacia = _cargada && Error is null && _todas.Count == 0;
        SinResultados = _todas.Count > 0 && visibles.Count == 0;
    }

    private async void MostrarAviso(string mensaje)
    {
        _finAviso?.Cancel();
        var cts = _finAviso = new CancellationTokenSource();
        Aviso = mensaje;
        try
        {
            await Task.Delay(3000, cts.Token);
            Aviso = null;
        }
        catch (TaskCanceledException)
        {
        }
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
            Error = MensajesError.Para(repo, repo.Message);
            // El ícono de "sin red" solo si de verdad el dispositivo está sin conexión
            IconoError = repo.SinConexion && Error != MensajesError.SinServidor ? Iconos.SinConexion : Iconos.Pelicula;
        }
        else
        {
            Error = "No se pudo cargar las películas";
            IconoError = Iconos.Pelicula;
        }
        Vacia = false;
    }

    private static async Task AvisarAsync(Exception ex)
    {
        Debug.WriteLine($"[PeliculasViewModel] {ex}");
        var mensaje = MensajesError.Para(ex, "No se pudo completar la operación");
        try
        {
            if (Shell.Current is { } shell)
                await shell.DisplayAlert("CineList", mensaje, "Aceptar");
        }
        catch (Exception alerta)
        {
            Debug.WriteLine($"[PeliculasViewModel] {alerta.Message}");
        }
    }
}
