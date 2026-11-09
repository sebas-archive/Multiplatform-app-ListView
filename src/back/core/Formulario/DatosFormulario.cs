using System.Globalization;
using CineList.Core.Models;

namespace CineList.Core.Formulario;

/// <summary>
/// Instantánea de los campos del formulario tal como los escribe el usuario (año y duración como texto).
/// Al ser un record, dos instantáneas iguales significan "sin cambios".
/// </summary>
public sealed record DatosFormulario(
    string Titulo,
    string Director,
    string Genero,
    string Anio,
    string Duracion,
    string Sinopsis,
    bool Vista,
    int Calificacion,
    string? PosterBase64)
{
    /// <summary>Formulario vacío (nueva película).</summary>
    public static DatosFormulario Vacio { get; } = new("", "", "", "", "", "", false, 0, null);

    /// <summary>Carga los campos de una película existente; año y duración en 0 quedan vacíos.</summary>
    public static DatosFormulario Desde(Pelicula p) => new(
        p.Titulo,
        p.Director,
        p.Genero,
        p.Anio > 0 ? p.Anio.ToString(CultureInfo.InvariantCulture) : "",
        p.DuracionMin > 0 ? p.DuracionMin.ToString(CultureInfo.InvariantCulture) : "",
        p.Sinopsis,
        p.Vista,
        p.Calificacion,
        string.IsNullOrEmpty(p.PosterBase64) ? null : p.PosterBase64);

    /// <summary>
    /// Construye una película NUEVA con estos datos (textos recortados, número vacío o inválido = 0,
    /// calificación limitada a 0–5). Conserva Id y CreadaEn de <paramref name="original"/> si se da;
    /// nunca modifica <paramref name="original"/>.
    /// </summary>
    public Pelicula CrearPelicula(Pelicula? original = null)
    {
        var p = original?.Clonar() ?? new Pelicula();
        p.Titulo = (Titulo ?? "").Trim();
        p.Director = (Director ?? "").Trim();
        p.Genero = (Genero ?? "").Trim();
        p.Anio = Entero(Anio);
        p.DuracionMin = Entero(Duracion);
        p.Sinopsis = (Sinopsis ?? "").Trim();
        p.Vista = Vista;
        p.Calificacion = Math.Clamp(Calificacion, 0, 5);
        p.PosterBase64 = string.IsNullOrEmpty(PosterBase64) ? null : PosterBase64;
        return p;
    }

    private static int Entero(string? texto)
        => int.TryParse(texto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
}
