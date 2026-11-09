using CineList.Core.Models;

namespace CineList.Services;

// Transición "póster que vuela": la lista guarda dónde estaba el póster tocado (coordenadas de ventana)
// y el detalle lo toma al aparecer. Un solo uso y caduca a los 2 s, para que una navegación
// fallida o tardía no haga volar un póster fuera de contexto.
public static class TransicionPoster
{
    private static readonly TimeSpan Caducidad = TimeSpan.FromSeconds(2);
    private static (string Id, Rect Origen, DateTime Hora)? _pendiente;

    public static void Preparar(Pelicula p, Rect origenEnVentana) =>
        _pendiente = (p.Id, origenEnVentana, DateTime.UtcNow);

    // Devuelve el origen si corresponde a esta película y no caducó; siempre lo consume
    public static bool Tomar(string id, out Rect origen)
    {
        var pendiente = _pendiente;
        _pendiente = null;
        origen = default;
        if (pendiente is not { } p || p.Id != id || DateTime.UtcNow - p.Hora > Caducidad) return false;
        origen = p.Origen;
        return true;
    }

    public static void Descartar() => _pendiente = null;
}
