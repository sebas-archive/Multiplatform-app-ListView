using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using CineList.Core.Models;

namespace CineList.Core.Firestore;

/// <summary>Acceso a la colección de películas mediante la API REST de Firestore.</summary>
public interface IPeliculasRepository
{
    Task<IReadOnlyList<Pelicula>> ListarAsync(CancellationToken ct = default);
    Task<Pelicula> CrearAsync(Pelicula p, CancellationToken ct = default);
    Task ActualizarAsync(Pelicula p, CancellationToken ct = default);
    Task EliminarAsync(string id, CancellationToken ct = default);
    Task CargarEjemplosAsync(CancellationToken ct = default);
}

/// <param name="pausaReintento">Espera antes del único reintento (por defecto 300 ms; las pruebas usan cero).</param>
public class PeliculasRepository(HttpClient http, TimeSpan? pausaReintento = null) : IPeliculasRepository
{
    private readonly TimeSpan _pausaReintento = pausaReintento ?? TimeSpan.FromMilliseconds(300);

    private const string Base =
        "https://firestore.googleapis.com/v1/projects/" + FirebaseConfig.ProjectId + "/databases/(default)/documents";
    private static readonly string Coleccion = $"{Base}/{FirebaseConfig.Coleccion}";
    private static readonly string Key = "key=" + FirebaseConfig.ApiKey;

    public async Task<IReadOnlyList<Pelicula>> ListarAsync(CancellationToken ct = default)
    {
        var resultado = new List<Pelicula>();
        string? token = null;
        do
        {
            var url = $"{Coleccion}?pageSize=300" +
                      (token is null ? "" : "&pageToken=" + Uri.EscapeDataString(token)) +
                      "&" + Key;
            var json = await EnviarAsync(HttpMethod.Get, url, null, ct, reintentar: true);
            if (json?["documents"] is JsonArray docs)
                foreach (var d in docs)
                    if (d is JsonObject o)
                        resultado.Add(FirestoreMapper.DesdeDocumento(o));
            token = (string?)json?["nextPageToken"];
        } while (!string.IsNullOrEmpty(token));

        return resultado.OrderByDescending(p => p.CreadaEn).ToList();
    }

    public async Task<Pelicula> CrearAsync(Pelicula p, CancellationToken ct = default)
    {
        var copia = p.Clonar();
        if (copia.CreadaEn == default) copia.CreadaEn = DateTimeOffset.UtcNow;
        var cuerpo = new JsonObject { ["fields"] = FirestoreMapper.ACampos(copia) };
        var json = await EnviarAsync(HttpMethod.Post, $"{Coleccion}?{Key}", cuerpo, ct);
        var nombre = (string?)json?["name"] ?? "";
        copia.Id = nombre[(nombre.LastIndexOf('/') + 1)..];
        return copia;
    }

    public async Task ActualizarAsync(Pelicula p, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(p.Id))
            throw new ArgumentException("La película no tiene Id.", nameof(p));
        var cuerpo = new JsonObject { ["fields"] = FirestoreMapper.ACampos(p) };
        await EnviarAsync(HttpMethod.Patch, $"{Coleccion}/{Uri.EscapeDataString(p.Id)}?{Key}", cuerpo, ct,
            reintentar: true);
    }

    public async Task EliminarAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id vacío.", nameof(id));
        await EnviarAsync(HttpMethod.Delete, $"{Coleccion}/{Uri.EscapeDataString(id)}?{Key}", null, ct,
            reintentar: true, ignorar404: true);
    }

    public async Task CargarEjemplosAsync(CancellationToken ct = default)
    {
        foreach (var ejemplo in EjemplosPeliculas.Crear(DateTimeOffset.UtcNow))
            await CrearAsync(ejemplo, ct);
    }

    // GET, PATCH y DELETE son idempotentes: ante un fallo de conexión (p. ej. una conexión reutilizada
    // que el servidor ya cerró) se reintentan UNA vez. POST (crear) nunca se reintenta: podría duplicar.
    // No se reintenta ante errores HTTP (el servidor respondió), ni timeouts, ni cancelación del llamador.
    private async Task<JsonObject?> EnviarAsync(HttpMethod metodo, string url, JsonObject? cuerpo,
        CancellationToken ct, bool reintentar = false, bool ignorar404 = false)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                return await EnviarUnaVezAsync(metodo, url, cuerpo, ct, ignorar404);
            }
            catch (Exception ex) when (EsFalloDeConexion(ex))
            {
                if (!reintentar || intento >= 2 || ct.IsCancellationRequested)
                    throw new RepositorioException("Sin conexión a internet", true, ex);
            }
            await Task.Delay(_pausaReintento, ct);
        }
    }

    private async Task<JsonObject?> EnviarUnaVezAsync(HttpMethod metodo, string url, JsonObject? cuerpo,
        CancellationToken ct, bool ignorar404)
    {
        try
        {
            // Cada intento crea su propia petición: un HttpRequestMessage no se puede reenviar
            using var req = new HttpRequestMessage(metodo, url);
            if (cuerpo is not null)
                req.Content = new StringContent(cuerpo.ToJsonString(), Encoding.UTF8, "application/json");

            using var resp = await http.SendAsync(req, ct);
            if (ignorar404 && resp.StatusCode == HttpStatusCode.NotFound) return null;
            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new RepositorioException("Sin permiso para acceder a la base de datos", false);
            if (!resp.IsSuccessStatusCode)
                throw new RepositorioException($"Error del servidor ({(int)resp.StatusCode})", false);

            var texto = await resp.Content.ReadAsStringAsync(ct);
            return string.IsNullOrWhiteSpace(texto) ? null : JsonNode.Parse(texto) as JsonObject;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Timeout de HttpClient: no se reintenta (duplicaría la espera)
            throw new RepositorioException("Sin conexión a internet", true, ex);
        }
    }

    // En Android el handler nativo informa la falta de red como WebException o IOException
    private static bool EsFalloDeConexion(Exception ex) => ex is HttpRequestException or WebException or IOException;
}
