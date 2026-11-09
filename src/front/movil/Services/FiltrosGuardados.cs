using CineList.Core.Lista;

namespace CineList.Services;

// Recuerda filtros y orden entre aperturas (Preferences, clave "filtros"). El texto de búsqueda no se guarda.
public class FiltrosGuardados
{
    private const string Clave = "filtros";

    public CriteriosFiltro Cargar() => CriteriosFiltro.DesdeJson(Preferences.Get(Clave, ""));

    public void Guardar(CriteriosFiltro criterios) => Preferences.Set(Clave, criterios.AJson());
}
