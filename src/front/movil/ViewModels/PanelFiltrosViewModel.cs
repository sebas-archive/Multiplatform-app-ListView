using System.Collections.ObjectModel;
using System.ComponentModel;
using CineList.Core.Lista;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineList.ViewModels;

// Una opción del panel (chip de género o década, segmento de orden)
public partial class OpcionChip : ObservableObject
{
    public OpcionChip(string texto, object? valor, bool seleccionado)
    {
        Texto = texto;
        Valor = valor;
        _seleccionado = seleccionado;
    }

    public string Texto { get; }
    public object? Valor { get; }

    [ObservableProperty] private bool _seleccionado;
}

// Borrador de filtros y orden del panel inferior. Se crea en cada apertura a partir de los criterios
// vigentes; nada llega a la lista hasta "Ver N películas". Texto y estado pertenecen a la barra de
// búsqueda y a los chips de la página: se leen de la lista y no se tocan.
public partial class PanelFiltrosViewModel : ObservableObject
{
    private readonly PeliculasViewModel lista;
    private bool _terminado; // Ver o Cerrar ya se ejecutó: se ignoran toques posteriores

    public PanelFiltrosViewModel(PeliculasViewModel lista)
    {
        this.lista = lista;
        var c = lista.Criterios;
        _ordenSeleccionado = c.Orden;
        _decada = c.Decada;
        _calificacionMinima = c.CalificacionMinima;

        Generos = new(GenerosDe(c).Select(g => new OpcionChip(g, g, c.Generos.Contains(g))));
        foreach (var g in Generos) g.PropertyChanged += OnGeneroCambiado;

        Decadas = new(Enum.GetValues<Decada>()
            .Select(d => new OpcionChip(DecadaTexto.Etiqueta(d), d, d == c.Decada)));

        // Etiquetas cortas para que los 5 segmentos quepan en el ancho del teléfono
        Ordenes = new(Enum.GetValues<OrdenPeliculas>()
            .Select(o => new OpcionChip(o == OrdenPeliculas.Calificacion ? "Calif." : OrdenPeliculasTexto.Etiqueta(o), o, o == c.Orden)));

        _conteo = FiltroPeliculas.Contar(lista.Todas, Borrador);
    }

    public ObservableCollection<OpcionChip> Generos { get; }
    public ObservableCollection<OpcionChip> Decadas { get; }
    public ObservableCollection<OpcionChip> Ordenes { get; }

    [ObservableProperty] private OrdenPeliculas _ordenSeleccionado;
    [ObservableProperty] private Decada _decada;
    [ObservableProperty] private int _calificacionMinima;
    [ObservableProperty] private int _conteo;

    public string TextoBoton => $"Ver {Conteo} {PalabraPeliculas}";
    public string PalabraPeliculas => Conteo == 1 ? "película" : "películas";
    public string TextoCalificacion => CalificacionMinima == 0 ? "Cualquiera" : $"{CalificacionMinima} o más";
    public int IndiceOrden => (int)OrdenSeleccionado;

    // La vista conecta aquí su animación de cierre; Ver la espera antes de aplicar para que la lista
    // y la insignia animen ya con el panel fuera
    public Func<Task>? CerrarVista { get; set; }

    // Criterios del borrador con el texto y el estado actuales de la página
    public CriteriosFiltro Borrador => new(
        lista.TextoBusqueda,
        lista.Estado,
        Generos.Where(g => g.Seleccionado).Select(g => g.Texto).ToHashSet(),
        Decada,
        CalificacionMinima,
        OrdenSeleccionado);

    partial void OnOrdenSeleccionadoChanged(OrdenPeliculas value)
    {
        foreach (var o in Ordenes) o.Seleccionado = (OrdenPeliculas)o.Valor! == value;
        OnPropertyChanged(nameof(IndiceOrden));
    }

    partial void OnDecadaChanged(Decada value)
    {
        foreach (var d in Decadas) d.Seleccionado = (Decada)d.Valor! == value;
        Recontar();
    }

    partial void OnCalificacionMinimaChanged(int value)
    {
        OnPropertyChanged(nameof(TextoCalificacion));
        Recontar();
    }

    partial void OnConteoChanged(int value)
    {
        OnPropertyChanged(nameof(TextoBoton));
        OnPropertyChanged(nameof(PalabraPeliculas));
    }

    private void OnGeneroCambiado(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OpcionChip.Seleccionado)) Recontar();
    }

    private void Recontar() => Conteo = FiltroPeliculas.Contar(lista.Todas, Borrador);

    [RelayCommand]
    private void AlternarGenero(OpcionChip? genero)
    {
        if (genero is not null) genero.Seleccionado = !genero.Seleccionado;
    }

    [RelayCommand]
    private void SeleccionarDecada(OpcionChip? decada)
    {
        if (decada?.Valor is Decada d) Decada = d;
    }

    [RelayCommand]
    private void SeleccionarOrden(OpcionChip? orden)
    {
        if (orden?.Valor is OrdenPeliculas o) OrdenSeleccionado = o;
    }

    // Restablece el borrador (la lista no cambia hasta "Ver")
    [RelayCommand]
    private void Limpiar()
    {
        OrdenSeleccionado = OrdenPeliculas.Genero;
        Decada = Decada.Todas;
        CalificacionMinima = 0;
        foreach (var g in Generos) g.Seleccionado = false;
    }

    // Aplica el borrador (también con 0 resultados: la lista muestra "Sin resultados") y cierra
    [RelayCommand]
    private async Task Ver()
    {
        if (_terminado) return;
        _terminado = true;
        var criterios = Borrador;
        if (CerrarVista is not null) await CerrarVista();
        lista.AplicarCriterios(criterios);
    }

    // Descarta el borrador
    [RelayCommand]
    private async Task Cerrar()
    {
        if (_terminado) return;
        _terminado = true;
        if (CerrarVista is not null) await CerrarVista();
    }

    // Géneros disponibles más los seleccionados que ya no tenga ninguna película (para poder quitarlos)
    private IEnumerable<string> GenerosDe(CriteriosFiltro c)
    {
        var disponibles = FiltroPeliculas.GenerosDisponibles(lista.Todas);
        // Conjunto con la comparación de géneros de Core (sin mayúsculas ni tildes)
        var conjunto = (CriteriosFiltro.Vacio with { Generos = disponibles.ToHashSet() }).Generos;
        return disponibles.Concat(c.Generos.Where(g => !conjunto.Contains(g)));
    }
}
