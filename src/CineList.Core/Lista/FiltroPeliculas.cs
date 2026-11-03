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

    /// <summary>Quita tildes y pasa a minúsculas.</summary>
    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    // Con datos de cultura (ICU) ignora tildes y mayúsculas; en modo invariante
    // la comparación nativa no lo hace, así que se usa el texto normalizado.
    private static readonly bool CulturaDisponible = Comparador.Compare("a", "Á", Opciones) == 0;

    private static int Comparar(string a, string b) =>
        CulturaDisponible
            ? Comparador.Compare(a, b, Opciones)
            : string.CompareOrdinal(Normalizar(a), Normalizar(b));

    private static readonly IEqualityComparer<string> Igualdad =
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
}
