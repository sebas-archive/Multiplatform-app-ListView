using System.Text.RegularExpressions;
using CineList.Core.Lista;
using CineList.Core.Models;
using CineList.Core.Poster;

namespace CineList.Core.Tests;

public class TextoPeliculaTests
{
    [Fact]
    public void Meta_CompletaUneConPuntoMedio() =>
        Assert.Equal("Nolan · 2014 · 169 min",
            TextoPelicula.Meta(new Pelicula { Director = " Nolan ", Anio = 2014, DuracionMin = 169 }));

    [Fact]
    public void Meta_OmiteParteVacias() =>
        Assert.Equal("2014", TextoPelicula.Meta(new Pelicula { Anio = 2014 }));

    [Fact]
    public void Meta_TodoVacioEsCadenaVacia() =>
        Assert.Equal("", TextoPelicula.Meta(new Pelicula()));

    [Fact]
    public void Etiqueta_Texto()
    {
        Assert.Equal("VISTA", TextoPelicula.Etiqueta(true));
        Assert.Equal("PENDIENTE", TextoPelicula.Etiqueta(false));
    }

    [Fact]
    public void PaletaHex_SeisParesValidos()
    {
        Assert.Equal(6, PosterGenerado.PaletaHex.Count);
        foreach (var par in PosterGenerado.PaletaHex)
        {
            Assert.Matches(new Regex("^#[0-9A-Fa-f]{6}$"), par.Inicio);
            Assert.Matches(new Regex("^#[0-9A-Fa-f]{6}$"), par.Fin);
        }
    }
}
