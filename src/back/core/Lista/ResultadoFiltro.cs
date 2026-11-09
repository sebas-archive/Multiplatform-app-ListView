namespace CineList.Core.Lista;

/// <summary>Resultado de aplicar criterios: grupos (uno solo, de género "", si es plano), total y resumen.</summary>
public sealed record ResultadoFiltro(IReadOnlyList<GrupoPeliculas> Grupos, bool Agrupado, int Total, string Resumen);
