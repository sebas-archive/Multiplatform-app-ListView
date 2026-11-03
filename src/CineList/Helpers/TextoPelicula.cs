using CineList.Core.Models;

namespace CineList.Helpers;

// Textos derivados de una película que comparten la lista y el detalle
public static class TextoPelicula
{
    // "Director · 2014 · 169 min" (omite las partes vacías)
    public static string Meta(Pelicula p) => string.Join(" · ", new[]
    {
        p.Director.Trim(),
        p.Anio > 0 ? p.Anio.ToString() : "",
        p.DuracionMin > 0 ? $"{p.DuracionMin} min" : "",
    }.Where(parte => parte.Length > 0));

    public static string Etiqueta(bool vista) => vista ? "VISTA" : "PENDIENTE";
}
