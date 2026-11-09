using System.Collections.Concurrent;

namespace CineList.Services;

// Convierte el póster base64 en ImageSource y lo cachea por id + longitud del texto,
// para no decodificar de nuevo cada vez que una celda se recicla.
public static class ImagenPoster
{
    private static readonly ConcurrentDictionary<string, (int Longitud, byte[] Bytes)> Cache = new();

    // Devuelve null si no hay póster (nulo o vacío) o si el base64 no es válido
    public static ImageSource? Desde(string? base64, string id)
    {
        if (string.IsNullOrEmpty(base64)) return null;

        byte[] bytes;
        if (Cache.TryGetValue(id, out var guardado) && guardado.Longitud == base64.Length)
        {
            bytes = guardado.Bytes;
        }
        else
        {
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(id)) Cache[id] = (base64.Length, bytes);
        }
        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    public static void Invalidar(string id) => Cache.TryRemove(id, out _);
}
