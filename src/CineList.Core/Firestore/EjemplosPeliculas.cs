using CineList.Core.Models;

namespace CineList.Core.Firestore;

/// <summary>Películas de ejemplo para poblar una lista vacía.</summary>
public static class EjemplosPeliculas
{
    public static IReadOnlyList<Pelicula> Crear(DateTimeOffset ahora) =>
    [
        Nueva("Interestelar", "Christopher Nolan", "Ciencia ficción", 2014, 169,
            "Un grupo de astronautas viaja a través de un agujero de gusano en busca de un nuevo hogar para la humanidad. Su misión pone a prueba el vínculo entre un padre y su hija.",
            true, 5, ahora, 0),
        Nueva("Dune: Parte Dos", "Denis Villeneuve", "Ciencia ficción", 2024, 166,
            "Paul Atreides se une a los fremen en el desierto mientras busca vengar a su familia. Debe elegir entre el amor y el destino que otros han trazado para él.",
            true, 4, ahora, 1),
        Nueva("El viaje de Chihiro", "Hayao Miyazaki", "Animación", 2001, 125,
            "Una niña queda atrapada en un mundo de espíritus y debe trabajar en unos baños para rescatar a sus padres. Allí aprende a ser valiente y a no perder su nombre.",
            true, 5, ahora, 2),
        Nueva("Coco", "Lee Unkrich", "Animación", 2017, 105,
            "Miguel sueña con ser músico aunque su familia lo tiene prohibido y acaba en la Tierra de los Muertos. Allí descubre el origen de sus antepasados.",
            false, 0, ahora, 3),
        Nueva("El Padrino", "Francis Ford Coppola", "Drama", 1972, 175,
            "La familia Corleone, una poderosa dinastía mafiosa, cambia de manos cuando el patriarca es herido. Su hijo menor, reacio al negocio, termina asumiendo el mando.",
            false, 0, ahora, 4),
        Nueva("Parásitos", "Bong Joon-ho", "Drama", 2019, 132,
            "Una familia humilde se infiltra poco a poco en la casa de unos millonarios. Lo que parece un plan ingenioso se complica de forma inesperada.",
            true, 4, ahora, 5)
    ];

    private static Pelicula Nueva(string titulo, string director, string genero, int anio, int duracion,
        string sinopsis, bool vista, int calificacion, DateTimeOffset ahora, int desplazamientoSeg) => new()
    {
        Titulo = titulo,
        Director = director,
        Genero = genero,
        Anio = anio,
        DuracionMin = duracion,
        Sinopsis = sinopsis,
        Vista = vista,
        Calificacion = calificacion,
        PosterBase64 = null,
        // Fechas distintas para conservar un orden estable al listar
        CreadaEn = ahora.AddSeconds(-desplazamientoSeg)
    };
}
