using CineList.Core.Formulario;
using CineList.Core.Models;

namespace CineList.Core.Tests;

public class DatosFormularioTests
{
    private static Pelicula Ejemplo() => new()
    {
        Id = "abc",
        Titulo = "Interestelar",
        Director = "Christopher Nolan",
        Genero = "Ciencia ficción",
        Anio = 2014,
        DuracionMin = 169,
        Sinopsis = "Viaje espacial.",
        Vista = true,
        Calificacion = 5,
        PosterBase64 = "QUJD",
        CreadaEn = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
    };

    [Fact]
    public void Desde_YCrear_SinCambios_DaPeliculaEquivalente()
    {
        var original = Ejemplo();
        var datos = DatosFormulario.Desde(original);
        var p = datos.CrearPelicula(original);

        Assert.NotSame(original, p);
        Assert.Equal(original.Id, p.Id);
        Assert.Equal(original.CreadaEn, p.CreadaEn);
        Assert.Equal(2014, p.Anio);
        Assert.Equal(169, p.DuracionMin);
        Assert.Equal("QUJD", p.PosterBase64);
    }

    [Fact]
    public void Desde_ConAnioYDuracionCero_DejaCamposVacios()
    {
        var datos = DatosFormulario.Desde(new Pelicula { Titulo = "X" });
        Assert.Equal("", datos.Anio);
        Assert.Equal("", datos.Duracion);
        Assert.Null(datos.PosterBase64);
    }

    [Fact]
    public void CrearPelicula_NoModificaLaOriginal()
    {
        var original = Ejemplo();
        var datos = DatosFormulario.Desde(original) with { Titulo = "Otro", PosterBase64 = null };
        var p = datos.CrearPelicula(original);

        Assert.Equal("Otro", p.Titulo);
        Assert.Null(p.PosterBase64);
        Assert.Equal("Interestelar", original.Titulo);
        Assert.Equal("QUJD", original.PosterBase64);
    }

    [Fact]
    public void CrearPelicula_RecortaTextos_YNumerosVaciosSonCero()
    {
        var datos = new DatosFormulario("  Ran  ", " Kurosawa ", " Drama ", "", "  ", "  ", false, 9, "");
        var p = datos.CrearPelicula();

        Assert.Equal("", p.Id);
        Assert.Equal("Ran", p.Titulo);
        Assert.Equal("Kurosawa", p.Director);
        Assert.Equal("Drama", p.Genero);
        Assert.Equal(0, p.Anio);
        Assert.Equal(0, p.DuracionMin);
        Assert.Equal("", p.Sinopsis);
        Assert.Equal(5, p.Calificacion);
        Assert.Null(p.PosterBase64);
    }

    [Fact]
    public void CrearPelicula_NumerosConEspacios_SeConvierten()
    {
        var p = (DatosFormulario.Vacio with { Titulo = "X", Anio = " 1985 ", Duracion = "162" }).CrearPelicula();
        Assert.Equal(1985, p.Anio);
        Assert.Equal(162, p.DuracionMin);
    }

    [Fact]
    public void Igualdad_DetectaCambios()
    {
        var inicial = DatosFormulario.Desde(Ejemplo());
        Assert.Equal(inicial, DatosFormulario.Desde(Ejemplo()));
        Assert.NotEqual(inicial, inicial with { Calificacion = 4 });
        Assert.NotEqual(inicial, inicial with { PosterBase64 = "WFla" });
        Assert.NotEqual(inicial, inicial with { Anio = "2015" });
        Assert.Equal(DatosFormulario.Vacio, new DatosFormulario("", "", "", "", "", "", false, 0, null));
    }
}
