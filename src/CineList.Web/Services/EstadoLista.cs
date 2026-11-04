using CineList.Core.Firestore;
using CineList.Core.Lista;
using CineList.Core.Models;
using Microsoft.JSInterop;

namespace CineList.Web.Services;

/// <summary>
/// Estado de la lista de películas (equivalente web de PeliculasViewModel). Singleton: el detalle y el
/// formulario usan Buscar/Upsert/Quitar para mantener la lista al día sin volver a descargarla.
/// Nunca muta instancias de <see cref="Todas"/>: clona, guarda y luego reemplaza.
/// </summary>
public class EstadoLista(IPeliculasRepository repositorio, AlmacenLocal almacen, IJSRuntime js)
{
    private const string ClaveFiltros = "filtros";
    public const string SinServidor = "No se pudo conectar con el servidor. Inténtalo de nuevo";

    private List<Pelicula> _todas = [];
    private bool _filtrosLeidos;
    private bool _ocupado;
    private CancellationTokenSource? _finAviso;
    private CancellationTokenSource? _finEntrada;
    private int _versionSalida;

    public event Action? Cambio;

    public IReadOnlyList<Pelicula> Todas => _todas;
    public CriteriosFiltro Criterios { get; private set; } = CriteriosFiltro.Vacio;
    public ResultadoFiltro Resultado { get; private set; } = FiltroPeliculas.Aplicar([], CriteriosFiltro.Vacio);

    /// <summary>Grupos a mostrar: los vacíos se omiten para que 0 resultados sea igual agrupado o plano.</summary>
    public IReadOnlyList<GrupoPeliculas> Grupos { get; private set; } = [];

    public bool Cargando { get; private set; }
    public bool Refrescando { get; private set; }
    public bool Cargada { get; private set; }
    public string? Error { get; private set; }
    public bool ErrorSinConexion { get; private set; }

    /// <summary>Mensaje breve no bloqueante (p. ej. falló el refresco pero la lista sigue visible).</summary>
    public string? Aviso { get; private set; }

    /// <summary>Cambia cada vez que la lista se reconstruye con entrada escalonada.</summary>
    public int VersionEntrada { get; private set; }

    /// <summary>True durante la entrada escalonada (luego se apaga para que agregar filas no la repita).</summary>
    public bool AnimarEntrada { get; private set; }

    /// <summary>True mientras la lista se desvanece antes de reconstruirse (la página pinta opacidad 0).</summary>
    public bool Saliendo { get; private set; }

    /// <summary>La página conecta aquí la espera del desvanecimiento (120 ms) antes de reconstruir.</summary>
    public Func<Task>? AnimarSalidaLista { get; set; }

    public bool Agrupado => Resultado.Agrupado;
    public bool Vacia => Cargada && Error is null && _todas.Count == 0;
    public bool SinResultados => _todas.Count > 0 && Resultado.Total == 0;
    public bool SinResultadosConFiltros => SinResultados && Criterios.TieneFiltrosDeContenido;
    public bool SinResultadosSimple => SinResultados && !Criterios.TieneFiltrosDeContenido;

    /// <summary>Limpiar filtros solo se ofrece si quitarlos devolvería resultados (si no, la causa es la búsqueda o el chip).</summary>
    public bool PuedeLimpiarFiltros =>
        Criterios.TieneFiltrosDeContenido
        && FiltroPeliculas.Contar(_todas, Criterios.SinFiltrosDeContenido()) > 0;

    public bool MostrarResumen => !Cargando && !Vacia && Error is null && !SinResultados;
    public string TextoActivos => FiltroPeliculas.TextoActivos(Criterios);

    /// <summary>Primera carga (con esqueleto). No hace nada si ya está cargada o cargando.</summary>
    public async Task IniciarAsync()
    {
        if (Cargada || _ocupado) return;
        await CargarAsync();
    }

    /// <summary>Descarga con esqueleto (primera carga y "Reintentar").</summary>
    public Task CargarAsync() => DescargarAsync(mostrarEsqueleto: true);

    /// <summary>Botón "Recargar": conserva la lista si falla y avisa sin bloquear.</summary>
    public Task RecargarAsync() => DescargarAsync(mostrarEsqueleto: _todas.Count == 0);

    public void CambiarTexto(string texto)
    {
        if (texto == Criterios.Texto) return;
        Criterios = Criterios with { Texto = texto };
        Aplicar(animar: false);
    }

    public async Task CambiarEstadoAsync(FiltroEstado estado)
    {
        if (estado == Criterios.Estado) return;
        Criterios = Criterios with { Estado = estado };
        await GuardarFiltrosAsync();
        await AplicarConSalidaAsync();
    }

    /// <summary>Criterios nuevos desde el panel: conserva el texto, guarda y re-filtra con salida y entrada.</summary>
    public void AplicarCriterios(CriteriosFiltro nuevos)
    {
        Criterios = nuevos with { Texto = Criterios.Texto };
        _ = GuardarFiltrosAsync();
        _ = AplicarConSalidaAsync();
    }

    public void LimpiarFiltros() => AplicarCriterios(Criterios.SinFiltrosDeContenido());

    public Pelicula? Buscar(string id) => _todas.FirstOrDefault(p => p.Id == id);

    /// <summary>Agrega o reemplaza una película (tras crear o editar) y re-aplica el filtro.</summary>
    public void Upsert(Pelicula pelicula)
    {
        var i = _todas.FindIndex(p => p.Id == pelicula.Id);
        if (i >= 0) _todas[i] = pelicula;
        else _todas.Insert(0, pelicula);
        Aplicar(animar: false);
    }

    public void Quitar(string id)
    {
        if (_todas.RemoveAll(p => p.Id == id) == 0) return;
        Aplicar(animar: false);
    }

    /// <summary>Alterna vista/pendiente: clona, guarda y reemplaza. Devuelve false si falló (con aviso).</summary>
    public async Task<bool> AlternarVistaAsync(Pelicula pelicula)
    {
        var copia = pelicula.Clonar();
        copia.Vista = !pelicula.Vista;
        try
        {
            await repositorio.ActualizarAsync(copia);
        }
        catch (Exception ex)
        {
            await AvisarFalloAsync(ex);
            return false;
        }
        Upsert(copia);
        return true;
    }

    /// <summary>Elimina (ya confirmado por la interfaz). Devuelve false si falló (con aviso).</summary>
    public async Task<bool> EliminarAsync(Pelicula pelicula)
    {
        try
        {
            await repositorio.EliminarAsync(pelicula.Id);
        }
        catch (Exception ex)
        {
            await AvisarFalloAsync(ex);
            return false;
        }
        Quitar(pelicula.Id);
        return true;
    }

    public async Task CargarEjemplosAsync()
    {
        if (_ocupado) return;
        _ocupado = true;
        Cargando = true;
        Notificar();
        try
        {
            await repositorio.CargarEjemplosAsync();
        }
        catch (Exception ex)
        {
            await MostrarErrorAsync(ex);
            Cargando = false;
            _ocupado = false;
            Notificar();
            return;
        }
        _ocupado = false;
        await DescargarAsync(mostrarEsqueleto: true);
    }

    private async Task DescargarAsync(bool mostrarEsqueleto)
    {
        if (_ocupado) return;
        _ocupado = true;
        // Con esqueleto se muestra al menos 600 ms: un reintento sin red falla al instante
        // y sin esta pausa no se notaría que se volvió a intentar
        var pausaMinima = Task.CompletedTask;
        try
        {
            if (!_filtrosLeidos)
            {
                _filtrosLeidos = true;
                var texto = Criterios.Texto;
                Criterios = CriteriosFiltro.DesdeJson(await almacen.LeerAsync(ClaveFiltros)) with { Texto = texto };
            }
            if (mostrarEsqueleto)
            {
                Error = null; // al reintentar se oculta el error y se ve el esqueleto
                Cargando = true;
            }
            else
            {
                Refrescando = true;
            }
            Notificar();
            if (mostrarEsqueleto) pausaMinima = Task.Delay(600);
            var lista = await repositorio.ListarAsync();
            await pausaMinima;
            _todas = lista.ToList();
            Cargada = true;
            Error = null;
            Cargando = false;
            Aplicar(animar: true);
        }
        catch (Exception ex) when (!mostrarEsqueleto && _todas.Count > 0)
        {
            // Falló el refresco con la lista ya cargada: se conserva y se avisa sin bloquear
            Console.WriteLine($"[EstadoLista] {ex}");
            MostrarAviso(await MensajeAsync(ex, "No se pudo actualizar la lista"));
        }
        catch (Exception ex)
        {
            await pausaMinima;
            await MostrarErrorAsync(ex);
        }
        finally
        {
            Cargando = false;
            Refrescando = false;
            _ocupado = false;
            Notificar();
        }
    }

    // Cambio de chip, filtro u orden: las filas actuales se desvanecen y luego se reconstruye con entrada
    private async Task AplicarConSalidaAsync()
    {
        var version = ++_versionSalida;
        if (AnimarSalidaLista is { } salida && Grupos.Count > 0)
        {
            Saliendo = true;
            Notificar();
            try { await salida(); }
            catch (Exception ex) { Console.WriteLine($"[EstadoLista] {ex.Message}"); }
            // Otro cambio posterior ya reconstruyó (y Aplicar apagó Saliendo); no se repite
            if (version != _versionSalida) return;
        }
        Aplicar(animar: true);
    }

    // Filtra, ordena y agrupa en memoria (sin red). Cualquier reconstrucción deja la lista visible:
    // apaga Saliendo antes de notificar, aunque interrumpa un desvanecimiento en curso
    private void Aplicar(bool animar)
    {
        _versionSalida++;
        Saliendo = false;
        Resultado = FiltroPeliculas.Aplicar(_todas, Criterios);
        Grupos = Resultado.Grupos.Where(g => g.Count > 0).ToList();
        if (animar) IniciarEntrada();
        Notificar();
    }

    // Activa la entrada escalonada y la apaga a los 800 ms (10 filas × 40 ms + 300 ms de animación)
    private async void IniciarEntrada()
    {
        _finEntrada?.Cancel();
        var cts = _finEntrada = new CancellationTokenSource();
        VersionEntrada++;
        AnimarEntrada = true;
        try
        {
            await Task.Delay(800, cts.Token);
            AnimarEntrada = false;
            Notificar();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void MostrarAviso(string mensaje)
    {
        _finAviso?.Cancel();
        var cts = _finAviso = new CancellationTokenSource();
        Aviso = mensaje;
        Notificar();
        try
        {
            await Task.Delay(3000, cts.Token);
            Aviso = null;
            Notificar();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task AvisarFalloAsync(Exception ex)
    {
        Console.WriteLine($"[EstadoLista] {ex}");
        MostrarAviso(await MensajeAsync(ex, "No se pudo completar la acción"));
    }

    private async Task MostrarErrorAsync(Exception ex)
    {
        Console.WriteLine($"[EstadoLista] {ex}");
        Error = await MensajeAsync(ex, "No se pudo cargar las películas");
        // El ícono de "sin red" solo si de verdad el navegador está sin conexión
        ErrorSinConexion = ex is RepositorioException { SinConexion: true } && Error != SinServidor;
    }

    /// <summary>
    /// Texto para el usuario: el de <see cref="RepositorioException"/>; un fallo de red con el navegador
    /// en línea (servidor caído, CORS) no culpa a la red del usuario.
    /// </summary>
    private async Task<string> MensajeAsync(Exception ex, string generico)
    {
        if (ex is not RepositorioException repo) return generico;
        return repo.SinConexion && await EnLineaAsync() ? SinServidor : repo.Message;
    }

    private async Task<bool> EnLineaAsync()
    {
        try { return await js.InvokeAsync<bool>("cinelist.enLinea"); }
        catch (JSException) { return false; }
    }

    private Task GuardarFiltrosAsync() => almacen.GuardarAsync(ClaveFiltros, Criterios.AJson());

    private void Notificar() => Cambio?.Invoke();
}
