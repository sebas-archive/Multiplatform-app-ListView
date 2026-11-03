using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using CineList.Core.Firestore;
using CineList.Core.Models;

namespace CineList.Core.Tests;

public class PeliculasRepositoryTests
{
    private const string Base = "https://firestore.googleapis.com/v1/projects/cinelist-7a4de/databases/(default)/documents";
    private const string Key = "AIzaSyBkCaRxSZDMS0uvn2IvqDWyjUDnhruJckE";

    private record Registro(HttpMethod Metodo, string Url, string? Cuerpo);

    private class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Registro> Peticiones { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var cuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Peticiones.Add(new Registro(request.Method, request.RequestUri!.OriginalString, cuerpo));
            return responder(request);
        }
    }

    private class HandlerQueLanza(Exception ex) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw ex;
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode codigo = HttpStatusCode.OK) =>
        new(codigo) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string Doc(string id, string titulo, string? creada = null) =>
        "{\"name\":\"projects/cinelist-7a4de/databases/(default)/documents/movies/" + id + "\",\"fields\":{" +
        "\"title\":{\"stringValue\":\"" + titulo + "\"}" +
        (creada is null ? "" : ",\"createdAt\":{\"timestampValue\":\"" + creada + "\"}") + "}}";

    private static (PeliculasRepository repo, HandlerFalso handler) Crear(Func<HttpRequestMessage, HttpResponseMessage> r)
    {
        var h = new HandlerFalso(r);
        return (new PeliculasRepository(new HttpClient(h)), h);
    }

    [Fact]
    public async Task ListarAsync_HaceGetConKeyYMapea()
    {
        var (repo, h) = Crear(_ => Json("{\"documents\":[" + Doc("a1", "Coco") + "]}"));
        var lista = await repo.ListarAsync();
        Assert.Single(lista);
        Assert.Equal("a1", lista[0].Id);
        Assert.Equal("Coco", lista[0].Titulo);
        Assert.Equal(HttpMethod.Get, h.Peticiones[0].Metodo);
        Assert.Equal($"{Base}/movies?pageSize=300&key={Key}", h.Peticiones[0].Url);
    }

    [Fact]
    public async Task ListarAsync_SigueNextPageToken()
    {
        var n = 0;
        var (repo, h) = Crear(_ => n++ == 0
            ? Json("{\"documents\":[" + Doc("a", "A") + "],\"nextPageToken\":\"tok1\"}")
            : Json("{\"documents\":[" + Doc("b", "B") + "]}"));
        var lista = await repo.ListarAsync();
        Assert.Equal(2, lista.Count);
        Assert.Equal(2, h.Peticiones.Count);
        Assert.Contains("pageToken=tok1", h.Peticiones[1].Url);
        Assert.Contains($"key={Key}", h.Peticiones[1].Url);
    }

    [Fact]
    public async Task ListarAsync_ColeccionVacia()
    {
        var (repo, _) = Crear(_ => Json("{}"));
        Assert.Empty(await repo.ListarAsync());
    }

    [Fact]
    public async Task ListarAsync_OrdenaPorCreadaEnDescendente()
    {
        var (repo, _) = Crear(_ => Json("{\"documents\":[" +
            Doc("viejo", "V", "2024-01-01T00:00:00Z") + "," +
            Doc("nuevo", "N", "2025-06-01T00:00:00Z") + "," +
            Doc("medio", "M", "2024-12-01T00:00:00Z") + "]}"));
        var lista = await repo.ListarAsync();
        Assert.Equal(new[] { "nuevo", "medio", "viejo" }, lista.Select(p => p.Id));
    }

    [Fact]
    public async Task CrearAsync_PostConCamposYDevuelveId()
    {
        var (repo, h) = Crear(_ => Json(Doc("nuevo1", "Coco")));
        var entrada = new Pelicula { Titulo = "Coco", Director = "Lee Unkrich" };
        var creada = await repo.CrearAsync(entrada);
        Assert.Equal("nuevo1", creada.Id);
        Assert.Equal("", entrada.Id);
        Assert.NotEqual(default, creada.CreadaEn);
        Assert.Equal(HttpMethod.Post, h.Peticiones[0].Metodo);
        Assert.Equal($"{Base}/movies?key={Key}", h.Peticiones[0].Url);
        var cuerpo = JsonNode.Parse(h.Peticiones[0].Cuerpo!)!;
        Assert.Equal("Coco", (string?)cuerpo["fields"]!["title"]!["stringValue"]);
    }

    [Fact]
    public async Task ActualizarAsync_PatchAlDocumento()
    {
        var (repo, h) = Crear(_ => Json("{}"));
        await repo.ActualizarAsync(new Pelicula { Id = "abc", Titulo = "X" });
        Assert.Equal(new HttpMethod("PATCH"), h.Peticiones[0].Metodo);
        Assert.Equal($"{Base}/movies/abc?key={Key}", h.Peticiones[0].Url);
        Assert.Contains("\"fields\"", h.Peticiones[0].Cuerpo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ActualizarAsync_IdVacio_Lanza(string id)
    {
        var (repo, _) = Crear(_ => Json("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.ActualizarAsync(new Pelicula { Id = id }));
    }

    [Fact]
    public async Task EliminarAsync_Delete()
    {
        var (repo, h) = Crear(_ => Json("{}"));
        await repo.EliminarAsync("abc");
        Assert.Equal(HttpMethod.Delete, h.Peticiones[0].Metodo);
        Assert.Equal($"{Base}/movies/abc?key={Key}", h.Peticiones[0].Url);
    }

    [Fact]
    public async Task EliminarAsync_404_NoLanza()
    {
        var (repo, _) = Crear(_ => Json("{}", HttpStatusCode.NotFound));
        await repo.EliminarAsync("abc");
    }

    [Fact]
    public async Task SinInternet_LanzaRepositorioExceptionSinConexion()
    {
        var inner = new HttpRequestException("sin red");
        var repo = new PeliculasRepository(new HttpClient(new HandlerQueLanza(inner)));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.ListarAsync());
        Assert.True(ex.SinConexion);
        Assert.Equal("Sin conexión a internet", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    // En Android el handler nativo lanza WebException/IOException en vez de HttpRequestException
    [Theory]
    [InlineData("web")]
    [InlineData("io")]
    public async Task SinInternetEnAndroid_LanzaRepositorioExceptionSinConexion(string tipo)
    {
        Exception inner = tipo == "web"
            ? new System.Net.WebException("Unable to resolve host")
            : new System.IO.IOException("Connection reset");
        var repo = new PeliculasRepository(new HttpClient(new HandlerQueLanza(inner)));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.ListarAsync());
        Assert.True(ex.SinConexion);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public async Task Timeout_LanzaRepositorioExceptionSinConexion()
    {
        var repo = new PeliculasRepository(new HttpClient(new HandlerQueLanza(new TaskCanceledException("timeout"))));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.CrearAsync(new Pelicula { Titulo = "x" }));
        Assert.True(ex.SinConexion);
    }

    [Fact]
    public async Task CancelacionDelLlamador_Propaga()
    {
        var repo = new PeliculasRepository(new HttpClient(new HandlerQueLanza(new TaskCanceledException())));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.ListarAsync(cts.Token));
    }

    [Fact]
    public async Task Error403_LanzaRepositorioExceptionConMensaje()
    {
        var (repo, _) = Crear(_ => Json("{}", HttpStatusCode.Forbidden));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.ListarAsync());
        Assert.False(ex.SinConexion);
        Assert.Contains("permiso", ex.Message);
    }

    [Fact]
    public async Task Error500_MensajeConCodigo()
    {
        var (repo, _) = Crear(_ => Json("{}", HttpStatusCode.InternalServerError));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.EliminarAsync("a"));
        Assert.False(ex.SinConexion);
        Assert.Equal("Error del servidor (500)", ex.Message);
    }

    [Fact]
    public async Task CargarEjemplosAsync_CreaSeis()
    {
        var (repo, h) = Crear(_ => Json(Doc("x", "T")));
        await repo.CargarEjemplosAsync();
        Assert.Equal(6, h.Peticiones.Count);
        Assert.All(h.Peticiones, p => Assert.Equal(HttpMethod.Post, p.Metodo));
    }

    [Fact]
    public void Ejemplos_SeisConDatosValidos()
    {
        var l = EjemplosPeliculas.Crear(DateTimeOffset.UtcNow);
        Assert.Equal(6, l.Count);
        Assert.Contains(l, p => p.Titulo == "Interestelar" && p.Anio == 2014 && p.DuracionMin == 169);
        Assert.Contains(l, p => p.Vista) ;
        Assert.Contains(l, p => !p.Vista);
        Assert.Contains(l, p => p.Calificacion == 0);
        Assert.All(l, p => Assert.False(string.IsNullOrWhiteSpace(p.Sinopsis)));
        Assert.All(l, p => Assert.Null(p.PosterBase64));
        Assert.Equal(6, l.Select(p => p.CreadaEn).Distinct().Count());
    }
    // --- Reintento único ante fallo de conexión (conexión reutilizada ya cerrada, etc.) ---

    // Lanza las excepciones dadas en orden y luego responde con el generador; cuenta las peticiones
    private class HandlerSecuencia(Func<HttpRequestMessage, HttpResponseMessage> responder, params Exception[] fallos)
        : HttpMessageHandler
    {
        private int _llamadas;
        public int Llamadas => _llamadas;
        public List<string?> Cuerpos { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var n = Interlocked.Increment(ref _llamadas);
            Cuerpos.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(ct));
            if (n <= fallos.Length) throw fallos[n - 1];
            return responder(request);
        }
    }

    private static (PeliculasRepository repo, HandlerSecuencia h) ConFallos(
        Func<HttpRequestMessage, HttpResponseMessage> responder, params Exception[] fallos)
    {
        var h = new HandlerSecuencia(responder, fallos);
        return (new PeliculasRepository(new HttpClient(h), TimeSpan.Zero), h);
    }

    [Fact]
    public async Task Listar_FalloDeConexionUnaVez_ReintentaYFunciona()
    {
        var (repo, h) = ConFallos(_ => Json("{\"documents\":[" + Doc("a1", "Coco") + "]}"),
            new HttpRequestException("Socket closed"));
        var lista = await repo.ListarAsync();
        Assert.Single(lista);
        Assert.Equal(2, h.Llamadas);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("web")]
    [InlineData("io")]
    public async Task Actualizar_FalloDeConexionUnaVez_ReintentaConElMismoCuerpo(string tipo)
    {
        Exception fallo = tipo switch
        {
            "http" => new HttpRequestException("Socket closed"),
            "web" => new WebException("Socket closed"),
            _ => new IOException("Connection reset"),
        };
        var (repo, h) = ConFallos(_ => Json("{}"), fallo);
        await repo.ActualizarAsync(new Pelicula { Id = "a1", Titulo = "Coco" });
        Assert.Equal(2, h.Llamadas);
        Assert.Equal(h.Cuerpos[0], h.Cuerpos[1]);
        Assert.Contains("Coco", h.Cuerpos[1]);
    }

    [Fact]
    public async Task Eliminar_FalloDeConexionUnaVez_Reintenta()
    {
        var (repo, h) = ConFallos(_ => new HttpResponseMessage(HttpStatusCode.OK), new IOException("reset"));
        await repo.EliminarAsync("a1");
        Assert.Equal(2, h.Llamadas);
    }

    [Fact]
    public async Task Crear_FalloDeConexion_NoReintenta()
    {
        var (repo, h) = ConFallos(_ => Json(Doc("nuevo", "X")), new HttpRequestException("Socket closed"));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.CrearAsync(new Pelicula { Titulo = "X" }));
        Assert.True(ex.SinConexion);
        Assert.Equal(1, h.Llamadas);
    }

    [Fact]
    public async Task Actualizar_DosFallosDeConexion_LanzaSinConexion()
    {
        var segundo = new HttpRequestException("sigue sin red");
        var (repo, h) = ConFallos(_ => Json("{}"), new HttpRequestException("sin red"), segundo);
        var ex = await Assert.ThrowsAsync<RepositorioException>(
            () => repo.ActualizarAsync(new Pelicula { Id = "a1", Titulo = "Coco" }));
        Assert.True(ex.SinConexion);
        Assert.Equal("Sin conexión a internet", ex.Message);
        Assert.Same(segundo, ex.InnerException);
        Assert.Equal(2, h.Llamadas);
    }

    [Fact]
    public async Task ErrorHttp_NoSeReintenta()
    {
        var (repo, h) = ConFallos(_ => Json("{}", HttpStatusCode.ServiceUnavailable));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.ListarAsync());
        Assert.False(ex.SinConexion);
        Assert.Equal(1, h.Llamadas);
    }

    [Fact]
    public async Task Timeout_NoSeReintenta()
    {
        var (repo, h) = ConFallos(_ => Json("{}"), new TaskCanceledException("timeout"));
        var ex = await Assert.ThrowsAsync<RepositorioException>(() => repo.ListarAsync());
        Assert.True(ex.SinConexion);
        Assert.Equal(1, h.Llamadas);
    }

    [Fact]
    public async Task CancelacionDelLlamador_NoSeReintenta()
    {
        using var cts = new CancellationTokenSource();
        var (repo, h) = ConFallos(_ =>
        {
            cts.Cancel();
            throw new HttpRequestException("cortado al cancelar");
        });
        await Assert.ThrowsAnyAsync<Exception>(() => repo.ListarAsync(cts.Token));
        Assert.Equal(1, h.Llamadas);
    }
}
