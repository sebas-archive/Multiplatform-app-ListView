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

public class PeliculasRepository(HttpClient http) : IPeliculasRepository
{
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
            var json = await EnviarAsync(HttpMethod.Get, url, null, ct);
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
        await EnviarAsync(HttpMethod.Patch, $"{Coleccion}/{Uri.EscapeDataString(p.Id)}?{Key}", cuerpo, ct);
    }

    public async Task EliminarAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id vacío.", nameof(id));
        await EnviarAsync(HttpMethod.Delete, $"{Coleccion}/{Uri.EscapeDataString(id)}?{Key}", null, ct,
            ignorar404: true);
    }

    public async Task CargarEjemplosAsync(CancellationToken ct = default)
    {
        foreach (var ejemplo in EjemplosPeliculas.Crear(DateTimeOffset.UtcNow))
            await CrearAsync(ejemplo, ct);
    }

    private async Task<JsonObject?> EnviarAsync(HttpMethod metodo, string url, JsonObject? cuerpo,
        CancellationToken ct, bool ignorar404 = false)
    {
        try
        {
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
        // En Android el handler nativo informa la falta de red como WebException o IOException
        catch (Exception ex) when (ex is HttpRequestException or WebException or IOException ||
                                   (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new RepositorioException("Sin conexión a internet", true, ex);
        }
    }
}
