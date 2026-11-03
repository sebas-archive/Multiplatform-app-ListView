using CineList.Core.Formulario;

namespace CineList.Core.Tests;

public class ValidadorPeliculaTests
{
    private const string ErrTitulo = "El título es obligatorio";
    private const string ErrAnio = "El año debe ser un número entre 1888 y 2031";
    private const string ErrDuracion = "La duración debe ser un número entre 1 y 999 minutos";

    [Fact]
    public void TituloSoloEspacios_DaErrorDeTitulo()
        => Assert.Contains(ErrTitulo, ValidadorPelicula.Validar("  ", "2014", "169", 2026));

    [Theory]
    [InlineData("abc")]
    [InlineData("1500")]
    [InlineData("2032")]
    public void AnioInvalido_DaErrorDeAnio(string anio)
        => Assert.Equal(new[] { ErrAnio }, ValidadorPelicula.Validar("X", anio, "", 2026));

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1000")]
    [InlineData("x")]
    public void DuracionInvalida_DaErrorDeDuracion(string d)
        => Assert.Equal(new[] { ErrDuracion }, ValidadorPelicula.Validar("X", "", d, 2026));

    [Theory]
    [InlineData("Dune", "2024", "166")]
    [InlineData("X", "2031", "")]
    [InlineData("X", "", "")]
    [InlineData("X", null, null)]
    [InlineData("X", " 1888 ", " 999 ")]
    public void EntradasValidas_SinErrores(string t, string? a, string? d)
        => Assert.Empty(ValidadorPelicula.Validar(t, a, d, 2026));

    [Fact]
    public void Orden_TituloAnioDuracion()
        => Assert.Equal(new[] { ErrTitulo, ErrAnio, ErrDuracion },
            ValidadorPelicula.Validar(null, "abc", "0", 2026));
}
