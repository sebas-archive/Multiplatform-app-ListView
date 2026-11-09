namespace CineList.Core.Lista;

/// <summary>Criterio de orden de la lista. Solo <see cref="Genero"/> agrupa.</summary>
public enum OrdenPeliculas { Genero, Titulo, Anio, Calificacion, Recientes }

public static class OrdenPeliculasTexto
{
    public static string Etiqueta(OrdenPeliculas o) => o switch
    {
        OrdenPeliculas.Genero => "Género",
        OrdenPeliculas.Titulo => "Título",
        OrdenPeliculas.Anio => "Año",
        OrdenPeliculas.Calificacion => "Calificación",
        OrdenPeliculas.Recientes => "Recientes",
        _ => o.ToString(),
    };
}
