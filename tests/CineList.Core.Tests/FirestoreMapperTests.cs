using System.Globalization;
using System.Text.Json.Nodes;
using CineList.Core.Firestore;
using CineList.Core.Models;

namespace CineList.Core.Tests;

public class FirestoreMapperTests
{
    [Fact]
    public void ACampos_UsaNombresYTiposFirestore()
    {
        var p = new Pelicula
        {
            Titulo = "Interestelar",
            Anio = 2014,
            Vista = true,
            Calificacion = 5,
            PosterBase64 = null,
            CreadaEn = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero)
        };

        var campos = FirestoreMapper.ACampos(p);

        Assert.Equal("Interestelar", (string?)campos["title"]!["stringValue"]);
        Assert.Equal("2014", (string?)campos["year"]!["integerValue"]);
        Assert.True((bool?)campos["watched"]!["booleanValue"]);
        Assert.True(((JsonObject)campos["poster"]!).ContainsKey("nullValue"));

        var marca = (string?)campos["createdAt"]!["timestampValue"];
        Assert.NotNull(marca);
        Assert.EndsWith("Z", marca);
        var analizada = DateTimeOffset.Parse(marca, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.Equal(p.CreadaEn, analizada);
    }

    [Fact]
    public void DesdeDocumento_IdaYVuelta()
    {
        var p = new Pelicula
        {
            Titulo = "El viaje de Chihiro",
            Director = "Hayao Miyazaki",
            Genero = "Animación",
            Anio = 2001,
            DuracionMin = 125,
            Sinopsis = "Una niña en el mundo de los espíritus.",
            Vista = true,
            Calificacion = 4,
            PosterBase64 = "QUJD",
            CreadaEn = new DateTimeOffset(2026, 11, 3, 12, 30, 15, 123, TimeSpan.Zero)
        };
        var doc = new JsonObject
        {
            ["name"] = "projects/cinelist-7a4de/databases/(default)/documents/movies/abc",
            ["fields"] = FirestoreMapper.ACampos(p)
        };

        var r = FirestoreMapper.DesdeDocumento(doc);

        Assert.Equal("abc", r.Id);
        Assert.Equal(p.Titulo, r.Titulo);
        Assert.Equal(p.Director, r.Director);
        Assert.Equal(p.Genero, r.Genero);
        Assert.Equal(p.Anio, r.Anio);
        Assert.Equal(p.DuracionMin, r.DuracionMin);
        Assert.Equal(p.Sinopsis, r.Sinopsis);
        Assert.Equal(p.Vista, r.Vista);
        Assert.Equal(p.Calificacion, r.Calificacion);
        Assert.Equal(p.PosterBase64, r.PosterBase64);
        Assert.Equal(p.CreadaEn, r.CreadaEn);
    }

    [Fact]
    public void DesdeDocumento_CamposFaltantes_UsaDefectos()
    {
        var doc = JsonNode.Parse("""
            {"name":"x/movies/zzz","fields":{"title":{"stringValue":"Solo título"}}}
            """)!.AsObject();

        var r = FirestoreMapper.DesdeDocumento(doc);

        Assert.Equal("zzz", r.Id);
        Assert.Equal("Solo título", r.Titulo);
        Assert.Equal("", r.Genero);
        Assert.Equal(0, r.Anio);
        Assert.False(r.Vista);
        Assert.Null(r.PosterBase64);
    }

    [Fact]
    public void DesdeDocumento_AceptaDoubleValueEnEnteros()
    {
        var doc = JsonNode.Parse("""
            {"name":"x/movies/d1","fields":{"title":{"stringValue":"T"},"year":{"doubleValue":2001}}}
            """)!.AsObject();

        var r = FirestoreMapper.DesdeDocumento(doc);

        Assert.Equal(2001, r.Anio);
    }
}
