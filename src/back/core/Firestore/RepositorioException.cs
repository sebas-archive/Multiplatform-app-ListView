namespace CineList.Core.Firestore;

/// <summary>Error de acceso a datos; <see cref="SinConexion"/> indica fallo de red.</summary>
public class RepositorioException(string mensaje, bool sinConexion, Exception? interna = null)
    : Exception(mensaje, interna)
{
    public bool SinConexion { get; } = sinConexion;
}
