namespace CineList.Core.Poster;

/// <summary>Datos deterministas para el póster de reemplazo (iniciales y color de paleta).</summary>
public static class PosterGenerado
{
    private static readonly HashSet<string> PalabrasVacias = new(StringComparer.OrdinalIgnoreCase)
    {
        "el", "la", "los", "las", "un", "una", "the", "a", "de"
    };

    /// <summary>Pares de colores (arriba, abajo) para el póster generado.</summary>
    public static IReadOnlyList<(string Inicio, string Fin)> PaletaHex { get; } =
    [
        ("#2B5876", "#4E4376"),
        ("#C94B4B", "#4B134F"),
        ("#3A7D44", "#1D3B2A"),
        ("#D38312", "#A83279"),
        ("#1F6F8B", "#0B2A3A"),
        ("#614385", "#516395"),
    ];

    public static string Iniciales(string titulo)
    {
        var palabras = (titulo ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 0) return "?";

        var significativas = palabras.Where(p => !PalabrasVacias.Contains(p)).Take(2).ToList();
        if (significativas.Count == 0) significativas.Add(palabras[0]);

        var letras = significativas
            .Select(p => p.FirstOrDefault(char.IsLetterOrDigit))
            .Where(c => c != '\0')
            .Select(char.ToUpperInvariant)
            .ToArray();

        return letras.Length == 0 ? "?" : new string(letras);
    }

    /// <summary>Índice estable en [0, tamanoPaleta) usando FNV-1a (no depende de GetHashCode).</summary>
    public static int IndicePaleta(string titulo, int tamanoPaleta)
    {
        if (tamanoPaleta <= 0) throw new ArgumentOutOfRangeException(nameof(tamanoPaleta));

        var normalizado = (titulo ?? string.Empty).Trim().ToUpperInvariant();
        uint hash = 2166136261;
        foreach (var c in normalizado)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return (int)(hash % (uint)tamanoPaleta);
    }
}
