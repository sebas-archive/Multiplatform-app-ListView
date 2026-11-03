using CineList.Core.Poster;

namespace CineList.Core.Tests;

public class PosterTests
{
    [Fact] public void Escalar_Horizontal() => Assert.Equal((600, 450), PosterPolicy.Escalar(4000, 3000));
    [Fact] public void Escalar_Vertical() => Assert.Equal((450, 600), PosterPolicy.Escalar(3000, 4000));
    [Fact] public void Escalar_PequenaNoSeAgranda() => Assert.Equal((300, 200), PosterPolicy.Escalar(300, 200));
    [Fact] public void Escalar_ExactoNoCambia() => Assert.Equal((600, 400), PosterPolicy.Escalar(600, 400));
    [Fact] public void Escalar_MinimoUno() => Assert.Equal((600, 1), PosterPolicy.Escalar(10000, 5));

    [Fact] public void CabeEnDocumento_Limite() => Assert.True(PosterPolicy.CabeEnDocumento(new string('A', 900_000)));
    [Fact] public void CabeEnDocumento_Excede() => Assert.False(PosterPolicy.CabeEnDocumento(new string('A', 900_001)));
    [Fact] public void CabeEnDocumento_Nulo() => Assert.False(PosterPolicy.CabeEnDocumento(null!));

    [Theory]
    [InlineData("El viaje de Chihiro", "VC")]
    [InlineData("Dune", "D")]
    [InlineData("", "?")]
    [InlineData("   ", "?")]
    [InlineData("the a", "T")]
    [InlineData("la casa de papel", "CP")]
    public void Iniciales(string titulo, string esperado) => Assert.Equal(esperado, PosterGenerado.Iniciales(titulo));

    [Fact]
    public void IndicePaleta_EstableYEnRango()
    {
        var a = PosterGenerado.IndicePaleta("Dune", 6);
        Assert.Equal(a, PosterGenerado.IndicePaleta("Dune", 6));
        Assert.InRange(a, 0, 5);
        Assert.Equal(a, PosterGenerado.IndicePaleta("  dune ", 6));
    }

    [Fact]
    public void IndicePaleta_TamanoInvalido() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PosterGenerado.IndicePaleta("Dune", 0));
}
