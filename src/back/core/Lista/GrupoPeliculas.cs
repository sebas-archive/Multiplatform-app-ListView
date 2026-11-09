using CineList.Core.Models;

namespace CineList.Core.Lista;

/// <summary>Grupo de películas de un mismo género.</summary>
public class GrupoPeliculas : List<Pelicula>
{
    public string Genero { get; }

    public GrupoPeliculas(string genero, IEnumerable<Pelicula> items) : base(items)
    {
        Genero = genero;
    }
}
