namespace CineList.Core.Poster;

/// <summary>Política de tamaño y compresión de pósters (Firestore limita el documento a ~1 MiB).</summary>
public static class PosterPolicy
{
    public const int LadoMaximo = 600;
    public const int CalidadJpeg = 70;
    public const int BytesObjetivo = 300_000;
    public const int BytesMaximo = 900_000;

    /// <summary>Escala conservando proporción para que el lado mayor sea LadoMaximo; no agranda.</summary>
    public static (int Ancho, int Alto) Escalar(int ancho, int alto)
    {
        var mayor = Math.Max(ancho, alto);
        if (mayor <= LadoMaximo) return (ancho, alto);

        var factor = (double)LadoMaximo / mayor;
        int Reducir(int lado) => lado == mayor
            ? LadoMaximo
            : Math.Max(1, (int)Math.Round(lado * factor, MidpointRounding.AwayFromZero));

        return (Reducir(ancho), Reducir(alto));
    }

    /// <summary>True si el base64 (ASCII, 1 char = 1 byte) cabe en BytesMaximo. Nulo o vacío → false.</summary>
    public static bool CabeEnDocumento(string base64)
        => !string.IsNullOrEmpty(base64) && base64.Length <= BytesMaximo;
}
