using System.Collections.Frozen;
using System.Text.Json;

namespace CineList.Core.Lista;

/// <summary>Criterios de filtro y orden de la lista. Inmutable; los géneros se comparan sin mayúsculas ni tildes.</summary>
public sealed record CriteriosFiltro(
    string Texto,
    FiltroEstado Estado,
    IReadOnlySet<string> Generos,
    Decada Decada,
    int CalificacionMinima,
    OrdenPeliculas Orden)
{
    private readonly IReadOnlySet<string> _generos = Congelar(Generos);

    public IReadOnlySet<string> Generos
    {
        get => _generos;
        init => _generos = Congelar(value);
    }

    public static CriteriosFiltro Vacio { get; } =
        new("", FiltroEstado.Todas, FrozenSet<string>.Empty, Decada.Todas, 0, OrdenPeliculas.Genero);

    private static IReadOnlySet<string> Congelar(IReadOnlySet<string>? origen) =>
        (origen ?? Enumerable.Empty<string>())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Select(g => g.Trim())
            .ToFrozenSet(FiltroPeliculas.Igualdad);

    /// <summary>Cantidad de grupos de criterios activos (texto y estado no cuentan).</summary>
    public int CuantosActivos =>
        (Generos.Count > 0 ? 1 : 0)
        + (Decada != Decada.Todas ? 1 : 0)
        + (CalificacionMinima > 0 ? 1 : 0)
        + (Orden != OrdenPeliculas.Genero ? 1 : 0);

    public bool Equals(CriteriosFiltro? otro) =>
        otro is not null
        && Texto == otro.Texto && Estado == otro.Estado && Decada == otro.Decada
        && CalificacionMinima == otro.CalificacionMinima && Orden == otro.Orden
        && Generos.Count == otro.Generos.Count
        && ((FrozenSet<string>)Generos).SetEquals(otro.Generos);

    public override int GetHashCode()
    {
        var generos = 0;
        foreach (var g in Generos) generos ^= FiltroPeliculas.Igualdad.GetHashCode(g);
        return HashCode.Combine(Texto, Estado, Decada, CalificacionMinima, Orden, generos);
    }

    private sealed class Dto
    {
        public int Estado { get; set; }
        public string[]? Generos { get; set; }
        public int Decada { get; set; }
        public int CalificacionMinima { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>Serializa los criterios para recordarlos (sin el texto de búsqueda).</summary>
    public string AJson() => JsonSerializer.Serialize(new Dto
    {
        Estado = (int)Estado,
        Generos = Generos.ToArray(),
        Decada = (int)Decada,
        CalificacionMinima = CalificacionMinima,
        Orden = (int)Orden,
    });

    /// <summary>Lee criterios guardados; ante null, vacío, corrupto o valores inválidos devuelve <see cref="Vacio"/>.</summary>
    public static CriteriosFiltro DesdeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Vacio;
        try
        {
            var dto = JsonSerializer.Deserialize<Dto>(json);
            if (dto is null
                || !Enum.IsDefined((FiltroEstado)dto.Estado)
                || !Enum.IsDefined((Decada)dto.Decada)
                || !Enum.IsDefined((OrdenPeliculas)dto.Orden))
                return Vacio;
            return new CriteriosFiltro(
                "", (FiltroEstado)dto.Estado,
                (dto.Generos ?? []).Where(g => g is not null).ToFrozenSet(),
                (Decada)dto.Decada, Math.Clamp(dto.CalificacionMinima, 0, 5), (OrdenPeliculas)dto.Orden);
        }
        catch (JsonException) { return Vacio; }
        catch (NotSupportedException) { return Vacio; }
    }
}
