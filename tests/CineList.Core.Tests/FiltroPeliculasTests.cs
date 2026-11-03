using CineList.Core.Lista;
using CineList.Core.Models;

namespace CineList.Core.Tests;

public class FiltroPeliculasTests
{
    private static Pelicula P(string titulo, string director = "", string genero = "Drama", bool vista = false) =>
        new() { Titulo = titulo, Director = director, Genero = genero, Vista = vista };

    private static List<Pelicula> Muestra() => new()
    {
        P("Interestelar", "Nolan"),
        P("El viaje de Chihiro", "Miyazaki"),
        P("Película de prueba", "X"),
    };

    [Fact]
    public void Filtrar_TextoIgnoraMayusculasYTildes()
    {
        var datos = Muestra();
        Assert.Single(FiltroPeliculas.Filtrar(datos, "INTERESTELAR", FiltroEstado.Todas));
        Assert.Single(FiltroPeliculas.Filtrar(datos, "miyazaki", FiltroEstado.Todas));
        Assert.Single(FiltroPeliculas.Filtrar(datos, "pelicula", FiltroEstado.Todas));
        Assert.Equal(3, FiltroPeliculas.Filtrar(datos, "  ", FiltroEstado.Todas).Count);
        Assert.Equal(3, FiltroPeliculas.Filtrar(datos, null, FiltroEstado.Todas).Count);
    }

    [Fact]
    public void Filtrar_PorEstado()
    {
        var datos = new List<Pelicula> { P("A", vista: true), P("B", vista: true), P("C") };
        Assert.Equal(2, FiltroPeliculas.Filtrar(datos, null, FiltroEstado.Vistas).Count);
        Assert.Single(FiltroPeliculas.Filtrar(datos, null, FiltroEstado.Pendientes));
        Assert.Equal(3, FiltroPeliculas.Filtrar(datos, null, FiltroEstado.Todas).Count);
    }

    [Fact]
    public void Filtrar_CombinaTextoYEstado()
    {
        var datos = new List<Pelicula> { P("Alien", vista: true), P("Aliens"), P("Rocky", vista: true) };
        var r = FiltroPeliculas.Filtrar(datos, "alien", FiltroEstado.Pendientes);
        Assert.Equal("Aliens", Assert.Single(r).Titulo);
    }

    [Fact]
    public void Agrupar_OrdenaGenerosAlfabeticamenteYTitulosDentro()
    {
        var datos = new List<Pelicula>
        {
            P("Zodiac", genero: "Drama"),
            P("Ángel", genero: "Drama"),
            P("Beta", genero: "Drama"),
            P("Totoro", genero: "Animación"),
        };
        var grupos = FiltroPeliculas.Agrupar(datos);
        Assert.Equal(new[] { "Animación", "Drama" }, grupos.Select(g => g.Genero));
        Assert.Equal(new[] { "Ángel", "Beta", "Zodiac" }, grupos[1].Select(p => p.Titulo));
    }

    [Fact]
    public void Agrupar_GeneroVacio_VaASinGeneroAlFinal()
    {
        var datos = new List<Pelicula> { P("A", genero: " "), P("B", genero: "Drama"), P("C", genero: "") };
        var grupos = FiltroPeliculas.Agrupar(datos);
        Assert.Equal(new[] { "Drama", "Sin género" }, grupos.Select(g => g.Genero));
        Assert.Equal(2, grupos[^1].Count);
    }

    [Theory]
    [InlineData(0, 0, "0 películas · 0 vistas")]
    [InlineData(1, 1, "1 película · 1 vista")]
    [InlineData(3, 2, "3 películas · 2 vistas")]
    public void Resumen_Pluraliza(int total, int vistas, string esperado)
    {
        var datos = Enumerable.Range(0, total).Select(i => P("T" + i, vista: i < vistas)).ToList();
        Assert.Equal(esperado, FiltroPeliculas.Resumen(datos));
    }
}
