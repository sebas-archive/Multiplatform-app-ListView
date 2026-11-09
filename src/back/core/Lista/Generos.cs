namespace CineList.Core.Lista;

/// <summary>Géneros fijos que ofrecen el formulario y el filtro.</summary>
public static class Generos
{
    public static IReadOnlyList<string> Fijos { get; } =
    [
        "Acción", "Animación", "Ciencia ficción", "Comedia", "Documental",
        "Drama", "Fantasía", "Romance", "Suspenso", "Terror",
    ];
}
