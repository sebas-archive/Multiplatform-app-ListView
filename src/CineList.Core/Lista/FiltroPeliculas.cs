using System.Globalization;
using System.Text;
using CineList.Core.Models;

namespace CineList.Core.Lista;

/// <summary>Filtrado, agrupación y resumen de la lista de películas.</summary>
public static class FiltroPeliculas
{
    public const string SinGenero = "Sin género";

    private static readonly CompareInfo Comparador = ObtenerComparador();

    private static CompareInfo ObtenerComparador()
    {
        try { return CultureInfo.GetCultureInfo("es-CO").CompareInfo; }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture.CompareInfo; }
    }

    private const CompareOptions Opciones = CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase;

    private const string Acentuadas = "àáâãäåçèéêëìíîïñòóôõöùúûüýÿ";
    private const string Base        = "aaaaaaceeeeiiiinooooouuuuyy";

    /// <summary>Quita tildes y pasa a minúsculas.</summary>
    internal static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            var minuscula = char.ToLowerInvariant(c);
            // Respaldo para entornos sin datos ICU, donde Normalize no descompone.
            var i = Acentuadas.IndexOf(minuscula);
            sb.Append(i >= 0 ? Base[i] : minuscula);
        }
        return sb.ToString();
    }

    // Con datos de cultura (ICU) ignora tildes y mayúsculas; en modo invariante
    // la comparación nativa no lo hace, así que se usa el texto normalizado.
    private static readonly bool CulturaDisponible = Comparador.Compare("a", "Á", Opciones) == 0;

    internal static int Comparar(string a, string b) =>
        CulturaDisponible
            ? Comparador.Compare(a, b, Opciones)
            : string.CompareOrdinal(Normalizar(a), Normalizar(b));

    internal static readonly IEqualityComparer<string> Igualdad =
        EqualityComparer<string>.Create((a, b) => Comparar(a!, b!) == 0, s => Normalizar(s).GetHashCode());

    public static IReadOnlyList<Pelicula> Filtrar(IEnumerable<Pelicula> todas, string? texto, FiltroEstado estado)
    {
        var consulta = Normalizar(texto?.Trim());
        return todas
            .Where(p => estado switch
            {
                FiltroEstado.Vistas => p.Vista,
                FiltroEstado.Pendientes => !p.Vista,
                _ => true,
            })
            .Where(p => consulta.Length == 0
                || Normalizar(p.Titulo).Contains(consulta, StringComparison.Ordinal)
                || Normalizar(p.Director).Contains(consulta, StringComparison.Ordinal))
            .ToList();
    }

    public static IReadOnlyList<GrupoPeliculas> Agrupar(IEnumerable<Pelicula> peliculas)
    {
        return peliculas
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Genero) ? SinGenero : p.Genero.Trim(),
                     Igualdad)
            .OrderBy(g => g.Key == SinGenero ? 1 : 0)
            .ThenBy(g => g.Key, Comparer<string>.Create(Comparar))
            .Select(g => new GrupoPeliculas(g.Key, g.OrderBy(p => p.Titulo, Comparer<string>.Create(Comparar))))
            .ToList();
    }

    public static string Resumen(IEnumerable<Pelicula> peliculas)
    {
        var lista = peliculas as ICollection<Pelicula> ?? peliculas.ToList();
        var total = lista.Count;
        var vistas = lista.Count(p => p.Vista);
        return $"{total} {(total == 1 ? "película" : "películas")} · {vistas} {(vistas == 1 ? "vista" : "vistas")}";
    }

    private static readonly IComparer<string> ComparadorTitulos = Comparer<string>.Create(Comparar);

    /// <summary>Aplica texto, estado, géneros, década y calificación; luego ordena según <see cref="CriteriosFiltro.Orden"/>.</summary>
    public static ResultadoFiltro Aplicar(IEnumerable<Pelicula> todas, CriteriosFiltro c)
    {
        var filtradas = Filtrar(todas, c.Texto, c.Estado)
            .Where(p => c.Generos.Count == 0
                || c.Generos.Contains(string.IsNullOrWhiteSpace(p.Genero) ? SinGenero : p.Genero.Trim()))
            .Where(p => DecadaTexto.Contiene(c.Decada, p.Anio))
            .Where(p => p.Calificacion >= c.CalificacionMinima)
            .ToList();
        var resumen = Resumen(filtradas);

        if (c.Orden == OrdenPeliculas.Genero)
            return new ResultadoFiltro(Agrupar(filtradas), true, filtradas.Count, resumen);

        var ordenadas = c.Orden switch
        {
            OrdenPeliculas.Titulo => filtradas.OrderBy(p => p.Titulo, ComparadorTitulos),
            OrdenPeliculas.Anio => filtradas.OrderByDescending(p => p.Anio).ThenBy(p => p.Titulo, ComparadorTitulos),
            OrdenPeliculas.Calificacion => filtradas.OrderByDescending(p => p.Calificacion).ThenBy(p => p.Titulo, ComparadorTitulos),
            _ => filtradas.OrderByDescending(p => p.CreadaEn).ThenBy(p => p.Titulo, ComparadorTitulos),
        };
        return new ResultadoFiltro([new GrupoPeliculas("", ordenadas)], false, filtradas.Count, resumen);
    }

    public static int Contar(IEnumerable<Pelicula> todas, CriteriosFiltro c) => Aplicar(todas, c).Total;

    /// <summary>Géneros fijos y de los datos, sin duplicados (ignora mayúsculas/tildes), con "Sin género" al final si hace falta.</summary>
    public static IReadOnlyList<string> GenerosDisponibles(IEnumerable<Pelicula> todas)
    {
        var lista = todas as ICollection<Pelicula> ?? todas.ToList();
        var vistos = new HashSet<string>(Igualdad) { SinGenero };
        var generos = new List<string>();
        foreach (var g in Generos.Fijos.Concat(lista.Select(p => p.Genero?.Trim() ?? "")))
            if (g.Length > 0 && vistos.Add(g)) generos.Add(g);
        generos.Sort(ComparadorTitulos);
        if (lista.Any(p => string.IsNullOrWhiteSpace(p.Genero) || Igualdad.Equals(p.Genero.Trim(), SinGenero)))
            generos.Add(SinGenero);
        return generos;
    }

    /// <summary>Descripción corta de los criterios activos; "" si no hay ninguno.</summary>
    public static string TextoActivos(CriteriosFiltro c)
    {
        var partes = new List<string>();
        if (c.Generos.Count > 0)
            partes.Add(string.Join(", ", c.Generos.OrderBy(g => g, ComparadorTitulos)));
        if (c.Decada != Decada.Todas) partes.Add(DecadaTexto.Etiqueta(c.Decada));
        if (c.CalificacionMinima > 0)
            partes.Add($"{c.CalificacionMinima}+ {(c.CalificacionMinima == 1 ? "estrella" : "estrellas")}");
        if (c.Orden != OrdenPeliculas.Genero) partes.Add($"orden {OrdenPeliculasTexto.Etiqueta(c.Orden)}");
        return string.Join(" · ", partes);
    }
}
