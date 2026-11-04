using CineList.Core.Lista;

namespace CineList.Core.Tests;

public class CriteriosFiltroTests
{
    private static CriteriosFiltro Completo(string texto = "x") => new(
        texto, FiltroEstado.Vistas, new HashSet<string> { "Ciencia ficción", "Drama" },
        Decada.D1990s, 3, OrdenPeliculas.Anio);

    [Fact]
    public void CuantosActivos()
    {
        Assert.Equal(0, CriteriosFiltro.Vacio.CuantosActivos);
        Assert.Equal(4, Completo().CuantosActivos);
        Assert.Equal(0, (CriteriosFiltro.Vacio with { Texto = "abc", Estado = FiltroEstado.Vistas }).CuantosActivos);
        Assert.Equal(1, (CriteriosFiltro.Vacio with { CalificacionMinima = 2 }).CuantosActivos);
    }

    [Fact]
    public void Igualdad_PorContenidoDeGeneros()
    {
        var a = Completo();
        var b = a with { Generos = new HashSet<string> { "drama", "CIENCIA FICCION" } };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, a with { Generos = new HashSet<string> { "Drama" } });
        Assert.NotEqual(a, a with { Orden = OrdenPeliculas.Titulo });
        Assert.Equal(CriteriosFiltro.Vacio, CriteriosFiltro.Vacio with { Generos = null! });
    }

    [Fact]
    public void Generos_NullSeNormalizaAVacio()
    {
        var c = new CriteriosFiltro("", FiltroEstado.Todas, null!, Decada.Todas, 0, OrdenPeliculas.Genero);
        Assert.Empty(c.Generos);
        Assert.Equal(CriteriosFiltro.Vacio, c);
    }

    [Fact]
    public void Json_IdaYVuelta_SinTexto()
    {
        var json = Completo("buscar").AJson();
        Assert.DoesNotContain("buscar", json);
        Assert.Equal(Completo(""), CriteriosFiltro.DesdeJson(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{basura")]
    [InlineData("{\"Orden\":99}")]
    [InlineData("{\"Decada\":-1}")]
    [InlineData("{\"Estado\":7}")]
    [InlineData("[1,2]")]
    [InlineData("null")]
    [InlineData("{\"Generos\":5}")]
    public void DesdeJson_Invalido_DevuelveVacio(string? json) =>
        Assert.Equal(CriteriosFiltro.Vacio, CriteriosFiltro.DesdeJson(json));

    [Fact]
    public void DesdeJson_CalificacionSeRecorta()
    {
        Assert.Equal(5, CriteriosFiltro.DesdeJson("{\"CalificacionMinima\":9}").CalificacionMinima);
        Assert.Equal(0, CriteriosFiltro.DesdeJson("{\"CalificacionMinima\":-3}").CalificacionMinima);
    }
}
