using System.Globalization;

namespace CineList.Core.Formulario;

/// <summary>Validación del formulario de agregar/editar película.</summary>
public static class ValidadorPelicula
{
    private const int AnioMinimo = 1888;
    private const int AnioFuturoPermitido = 5;
    private const int DuracionMaxima = 999;

    /// <summary>
    /// Devuelve los errores en orden título, año, duración (a lo sumo uno por campo).
    /// Año y duración vacíos son válidos.
    /// </summary>
    public static IReadOnlyList<string> Validar(string? titulo, string? anio, string? duracion, int anioActual)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(titulo))
            errores.Add("El título es obligatorio");

        var maxAnio = anioActual + AnioFuturoPermitido;
        if (!EsEnteroEnRango(anio, AnioMinimo, maxAnio))
            errores.Add($"El año debe ser un número entre {AnioMinimo} y {maxAnio}");

        if (!EsEnteroEnRango(duracion, 1, DuracionMaxima))
            errores.Add($"La duración debe ser un número entre 1 y {DuracionMaxima} minutos");

        return errores;
    }

    private static bool EsEnteroEnRango(string? texto, int min, int max)
    {
        var limpio = texto?.Trim();
        if (string.IsNullOrEmpty(limpio)) return true;
        return int.TryParse(limpio, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor)
               && valor >= min && valor <= max;
    }
}
