using CineList.Core.Firestore;

namespace CineList.Helpers;

// Texto para mostrar al usuario a partir de un error del repositorio.
public static class MensajesError
{
    public const string SinServidor = "No se pudo conectar con el servidor. Inténtalo de nuevo";

    // Un fallo de conexión con el dispositivo en línea (servidor caído, conexión cortada) no es
    // "sin internet": se usa un texto que no culpe a la red del usuario.
    public static string Para(Exception ex, string generico)
    {
        if (ex is not RepositorioException repo) return generico;
        return repo.SinConexion && HayInternet() ? SinServidor : repo.Message;
    }

    private static bool HayInternet()
    {
        try
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }
        catch
        {
            return false;
        }
    }
}
