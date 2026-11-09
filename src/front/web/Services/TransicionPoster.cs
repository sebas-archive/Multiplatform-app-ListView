using System.Text.RegularExpressions;

namespace CineList.Web.Services;

/// <summary>Nombres para la transición del póster lista ↔ detalle (View Transitions).</summary>
public static partial class TransicionPoster
{
    /// <summary>Id apto para un nombre CSS y un selector de atributo: solo [A-Za-z0-9_-].</summary>
    public static string IdSeguro(string id) => NoPermitidos().Replace(id, "_");

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex NoPermitidos();
}
