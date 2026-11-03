namespace CineList.Core.Models;

/// <summary>Película de la lista del usuario.</summary>
public class Pelicula
{
    public string Id { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Director { get; set; } = "";
    public string Genero { get; set; } = "";
    public int Anio { get; set; }
    public int DuracionMin { get; set; }
    public string Sinopsis { get; set; } = "";
    public bool Vista { get; set; }
    public int Calificacion { get; set; }
    public string? PosterBase64 { get; set; }
    public DateTimeOffset CreadaEn { get; set; }

    /// <summary>Copia superficial de la película.</summary>
    public Pelicula Clonar() => (Pelicula)MemberwiseClone();
}
