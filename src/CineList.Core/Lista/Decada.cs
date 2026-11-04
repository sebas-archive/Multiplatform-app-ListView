namespace CineList.Core.Lista;

/// <summary>Filtro por década de estreno.</summary>
public enum Decada { Todas, Antes1980, D1980s, D1990s, D2000s, D2010s, D2020s }

public static class DecadaTexto
{
    public static string Etiqueta(Decada d) => d switch
    {
        Decada.Todas => "Todas",
        Decada.Antes1980 => "Antes de 1980",
        Decada.D1980s => "1980s",
        Decada.D1990s => "1990s",
        Decada.D2000s => "2000s",
        Decada.D2010s => "2010s",
        Decada.D2020s => "2020s",
        _ => d.ToString(),
    };

    /// <summary>Año 0 (sin dato) solo pasa con <see cref="Decada.Todas"/>; 2020s incluye todo año ≥ 2020.</summary>
    public static bool Contiene(Decada d, int anio) => d switch
    {
        Decada.Todas => true,
        Decada.Antes1980 => anio > 0 && anio < 1980,
        Decada.D1980s => anio is >= 1980 and < 1990,
        Decada.D1990s => anio is >= 1990 and < 2000,
        Decada.D2000s => anio is >= 2000 and < 2010,
        Decada.D2010s => anio is >= 2010 and < 2020,
        Decada.D2020s => anio >= 2020,
        _ => false,
    };
}
