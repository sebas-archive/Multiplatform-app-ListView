using CineList.Core.Lista;
using CineList.Core.Models;

namespace CineList.Core.Tests;

public class FiltroAvanzadoTests
{
    private static Pelicula P(string titulo, string genero, int anio, int cal, bool vista = false, int dia = 1, string director = "") =>
        new()
        {
            Titulo = titulo, Genero = genero, Anio = anio, Calificacion = cal, Vista = vista, Director = director,
            CreadaEn = new DateTimeOffset(2025, 1, dia, 0, 0, 0, TimeSpan.Zero),
        };

    private static List<Pelicula> Muestra() => new()
    {
        P("Interestelar", "Ciencia ficción", 2014, 5, true, 1, "Nolan"),
        P("Matrix", "ciencia ficcion", 1999, 4, true, 2),
        P("Chihiro", "Animación", 2001, 5, false, 3, "Miyazaki"),
        P("Alien", "Terror", 1979, 3, true, 4),
        P("Avatar", "Acción", 2022, 4, false, 5),
        P("Sin datos", "", 0, 0, false, 6),
        P("Zeta", "Drama", 2031, 4, false, 7),
        P("Beta", "Drama", 1980, 4, false, 8),
    };

    private static CriteriosFiltro C(
        string texto = "", FiltroEstado estado = FiltroEstado.Todas, string[]? generos = null,
        Decada decada = Decada.Todas, int min = 0, OrdenPeliculas orden = OrdenPeliculas.Genero) =>
        new(texto, estado, generos is null ? new HashSet<string>() : new HashSet<string>(generos), decada, min, orden);

    private static string N(string s) =>
        new string(s.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();

    private static string[] Titulos(ResultadoFiltro r) => r.Grupos.SelectMany(g => g).Select(p => p.Titulo).ToArray();

    [Fact]
    public void Aplicar_Vacio_EquivaleAAgruparActual()
    {
        var datos = Muestra();
        var r = FiltroPeliculas.Aplicar(datos, CriteriosFiltro.Vacio);
        var esperado = FiltroPeliculas.Agrupar(FiltroPeliculas.Filtrar(datos, "", FiltroEstado.Todas));
        Assert.True(r.Agrupado);
        Assert.Equal(datos.Count, r.Total);
        Assert.Equal(esperado.Select(g => g.Genero), r.Grupos.Select(g => g.Genero));
        Assert.Equal(esperado.SelectMany(g => g).Select(p => p.Titulo), Titulos(r));
    }

    [Fact]
    public void Aplicar_Generos_MultiplesYSinTildes()
    {
        var r = FiltroPeliculas.Aplicar(Muestra(), C(generos: ["ciencia ficcion", "TERROR"]));
        Assert.Equivalent(new[] { "Interestelar", "Matrix", "Alien" }, Titulos(r));
        Assert.Equal(3, r.Total);
    }

    [Fact]
    public void Aplicar_SinGenero_SeleccionaVacios()
    {
        var r = FiltroPeliculas.Aplicar(Muestra(), C(generos: [FiltroPeliculas.SinGenero]));
        Assert.Equal(["Sin datos"], Titulos(r));
    }

    [Theory]
    [InlineData(Decada.Antes1980, "Alien")]
    [InlineData(Decada.D1980s, "Beta")]
    [InlineData(Decada.D2010s, "Interestelar")]
    [InlineData(Decada.D2020s, "Avatar,Zeta")]
    public void Aplicar_Decada_Bordes(Decada d, string esperados)
    {
        var r = FiltroPeliculas.Aplicar(Muestra(), C(decada: d, orden: OrdenPeliculas.Titulo));
        Assert.Equal(esperados.Split(','), Titulos(r));
    }

    [Fact]
    public void Aplicar_Decada_Anio0SoloConTodas()
    {
        Assert.Contains("Sin datos", Titulos(FiltroPeliculas.Aplicar(Muestra(), C())));
        foreach (var d in Enum.GetValues<Decada>().Where(d => d != Decada.Todas))
            Assert.DoesNotContain("Sin datos", Titulos(FiltroPeliculas.Aplicar(Muestra(), C(decada: d))));
        Assert.True(DecadaTexto.Contiene(Decada.Antes1980, 1979));
        Assert.False(DecadaTexto.Contiene(Decada.Antes1980, 1980));
        Assert.True(DecadaTexto.Contiene(Decada.D2020s, 2031));
    }

    [Fact]
    public void Aplicar_CalificacionMinima()
    {
        var datos = Muestra();
        Assert.Equal(6, FiltroPeliculas.Aplicar(datos, C(min: 4)).Total);
        Assert.All(FiltroPeliculas.Aplicar(datos, C(min: 4)).Grupos.SelectMany(g => g), p => Assert.True(p.Calificacion >= 4));
        Assert.Equal(datos.Count, FiltroPeliculas.Aplicar(datos, C(min: 0)).Total);
        Assert.DoesNotContain("Sin datos", Titulos(FiltroPeliculas.Aplicar(datos, C(min: 1))));
    }

    [Fact]
    public void Aplicar_OrdenTitulo_Plano()
    {
        var r = FiltroPeliculas.Aplicar(Muestra(), C(orden: OrdenPeliculas.Titulo));
        Assert.False(r.Agrupado);
        var g = Assert.Single(r.Grupos);
        Assert.Equal("", g.Genero);
        Assert.Equal(["Alien", "Avatar", "Beta", "Chihiro", "Interestelar", "Matrix", "Sin datos", "Zeta"], Titulos(r));
    }

    [Fact]
    public void Aplicar_OrdenAnio_DescEmpateTitulo()
    {
        var datos = new List<Pelicula> { P("B", "x", 2000, 1), P("A", "x", 2000, 1), P("C", "x", 2010, 1), P("Z", "x", 0, 1) };
        Assert.Equal(["C", "A", "B", "Z"], Titulos(FiltroPeliculas.Aplicar(datos, C(orden: OrdenPeliculas.Anio))));
    }

    [Fact]
    public void Aplicar_OrdenCalificacion_DescEmpateTitulo()
    {
        var datos = new List<Pelicula> { P("B", "x", 1, 4), P("Á", "x", 1, 4), P("C", "x", 1, 5), P("Z", "x", 1, 0) };
        Assert.Equal(["C", "Á", "B", "Z"], Titulos(FiltroPeliculas.Aplicar(datos, C(orden: OrdenPeliculas.Calificacion))));
    }

    [Fact]
    public void Aplicar_OrdenRecientes_PorCreadaEn()
    {
        var r = FiltroPeliculas.Aplicar(Muestra(), C(orden: OrdenPeliculas.Recientes));
        Assert.False(r.Agrupado);
        Assert.Equal("", Assert.Single(r.Grupos).Genero);
        Assert.Equal(["Beta", "Zeta", "Sin datos", "Avatar", "Alien", "Chihiro", "Matrix", "Interestelar"], Titulos(r));
    }

    [Fact]
    public void Aplicar_CombinaTextoEstadoYFiltros()
    {
        var r = FiltroPeliculas.Aplicar(Muestra(),
            C(texto: "NOLAN", estado: FiltroEstado.Vistas, generos: ["Ciencia ficción"], decada: Decada.D2010s, min: 5));
        Assert.Equal(["Interestelar"], Titulos(r));
        Assert.Empty(FiltroPeliculas.Aplicar(Muestra(), C(texto: "nolan", estado: FiltroEstado.Pendientes)).Grupos);
    }

    [Fact]
    public void Contar_IgualATotal()
    {
        var c = C(generos: ["Drama"], min: 4);
        Assert.Equal(FiltroPeliculas.Aplicar(Muestra(), c).Total, FiltroPeliculas.Contar(Muestra(), c));
        Assert.Equal(2, FiltroPeliculas.Contar(Muestra(), c));
    }

    [Fact]
    public void Resumen_SeCalculaSobreFiltradas()
    {
        var r = FiltroPeliculas.Aplicar(Muestra(), C(generos: ["Ciencia ficción"]));
        Assert.Equal("2 películas · 2 vistas", r.Resumen);
        Assert.Equal("1 película · 0 vistas", FiltroPeliculas.Aplicar(Muestra(), C(generos: ["Acción"])).Resumen);
    }

    [Fact]
    public void GenerosDisponibles_UneFijosYDatos_SinDuplicados_SinGeneroAlFinal()
    {
        var datos = new List<Pelicula>
        {
            P("a", "ACCION", 1, 1), P("b", "Western", 1, 1), P("c", "western", 1, 1), P("d", "", 1, 1),
        };
        var r = FiltroPeliculas.GenerosDisponibles(datos);
        Assert.Equal(Generos.Fijos.Count + 1 + 1, r.Count);
        Assert.Equal(FiltroPeliculas.SinGenero, r[^1]);
        Assert.Single(r, g => N(g) == "accion");
        Assert.Contains("Acción", r);
        Assert.Single(r, g => N(g) == "western");
        Assert.Contains("Western", r);
        Assert.Equal(r.Take(r.Count - 1).OrderBy(g => N(g), StringComparer.Ordinal), r.Take(r.Count - 1));
        Assert.DoesNotContain(FiltroPeliculas.SinGenero, FiltroPeliculas.GenerosDisponibles([P("a", "Drama", 1, 1)]));
    }

    [Fact]
    public void TextoActivos_Formato()
    {
        var c = C(generos: ["Ciencia ficción", "Animación"], decada: Decada.D2000s, min: 4, orden: OrdenPeliculas.Calificacion);
        Assert.Equal("Animación, Ciencia ficción · 2000s · 4+ estrellas · orden Calificación", FiltroPeliculas.TextoActivos(c));
        Assert.Equal("", FiltroPeliculas.TextoActivos(CriteriosFiltro.Vacio));
        Assert.Equal("orden Título", FiltroPeliculas.TextoActivos(C(orden: OrdenPeliculas.Titulo)));
        Assert.Equal("1+ estrella", FiltroPeliculas.TextoActivos(C(min: 1)));
    }

    [Fact]
    public void Etiquetas()
    {
        Assert.Equal(["Género", "Título", "Año", "Calificación", "Recientes"],
            Enum.GetValues<OrdenPeliculas>().Select(OrdenPeliculasTexto.Etiqueta));
        Assert.Equal(["Todas", "Antes de 1980", "1980s", "1990s", "2000s", "2010s", "2020s"],
            Enum.GetValues<Decada>().Select(DecadaTexto.Etiqueta));
    }
}
