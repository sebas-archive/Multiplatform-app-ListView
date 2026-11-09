using CineList.Helpers;

namespace CineList.Services;

// Tema claro/oscuro. La preferencia se guarda en Preferences ("tema": "claro" | "oscuro");
// sin valor se sigue el tema del sistema.
public class TemaService
{
    private const string ClaveTema = "tema";

    public AppTheme TemaActual
    {
        get
        {
            var app = Application.Current;
            if (app is null) return AppTheme.Light;
            return app.UserAppTheme != AppTheme.Unspecified ? app.UserAppTheme : app.PlatformAppTheme;
        }
    }

    public void Inicializar()
    {
        if (Application.Current is not { } app) return;
        app.UserAppTheme = Preferences.Get(ClaveTema, "") switch
        {
            "claro" => AppTheme.Light,
            "oscuro" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    public async Task AlternarAsync(VisualElement contenido)
    {
        if (Application.Current is not { } app) return;
        var nuevo = TemaActual == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        Preferences.Set(ClaveTema, nuevo == AppTheme.Dark ? "oscuro" : "claro");

        if (Animaciones.ReducirMovimiento)
        {
            app.UserAppTheme = nuevo;
            return;
        }
        await contenido.FadeTo(0, 150, Easing.CubicOut);
        app.UserAppTheme = nuevo;
        await contenido.FadeTo(1, 150, Easing.CubicOut);
    }
}
