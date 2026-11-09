using CineList.Helpers;

namespace CineList.Controls;

// Ráfaga de partículas: círculos pequeños que salen en todas direcciones desde un punto, se
// desvanecen y se encogen. Todos se quitan del contenedor al terminar, pase lo que pase.
public static class Rafaga
{
    private const uint DuracionMs = 420;

    // Color de acento del tema actual (token Acento)
    public static Color ColorAcento()
    {
        var oscuro = (Application.Current?.RequestedTheme ?? AppTheme.Dark) == AppTheme.Dark;
        return Application.Current?.Resources.TryGetValue(oscuro ? "AcentoOscuro" : "AcentoClaro", out var v) == true && v is Color c
            ? c : Colors.Gold;
    }

    // Dónde nace la ráfaga respecto al elemento de origen
    public enum PuntoOrigen { Centro, BordeSuperior }

    // Ráfaga desde un elemento hacia una capa (convierte coordenadas de ventana a las de la capa).
    // arco: ángulos en grados (0 = derecha, 90 = abajo, -90 = arriba). Sin layout o con reducir movimiento no hace nada.
    public static Task Desde(VisualElement origen, Layout capa, PuntoOrigen punto, int puntos, double arcoMin, double arcoMax,
        double distanciaMin = 40, double distanciaMax = 60)
    {
        if (Animaciones.ReducirMovimiento) return Task.CompletedTask;
        if (Coordenadas.EnVentana(origen) is not { } r || Coordenadas.EnVentana(capa) is not { } c) return Task.CompletedTask;
        var y = punto == PuntoOrigen.BordeSuperior ? r.Top : r.Center.Y;
        return Lanzar(capa, new Point(r.Center.X - c.X, y - c.Y), ColorAcento(), puntos,
            arcoMin, arcoMax, distanciaMin, distanciaMax);
    }

    // centro: en coordenadas del contenedor. Los ángulos (grados) se reparten por igual en el arco.
    // Con reducir movimiento no hace nada.
    public static async Task Lanzar(Layout contenedor, Point centro, Color color, int puntos = 8,
        double arcoMin = 0, double arcoMax = 360, double distanciaMin = 40, double distanciaMax = 60)
    {
        if (Animaciones.ReducirMovimiento || puntos <= 0) return;
        var creadas = new List<BoxView>();
        try
        {
            var azar = Random.Shared;
            var paso = (arcoMax - arcoMin) / puntos;
            var tareas = new List<Task>();
            for (var i = 0; i < puntos; i++)
            {
                // Ángulos repartidos por igual con un pequeño desvío al azar
                var angulo = (arcoMin + (i + 0.5) * paso + (azar.NextDouble() - 0.5) * paso * 0.4) * Math.PI / 180;
                var distancia = distanciaMin + azar.NextDouble() * (distanciaMax - distanciaMin);
                double tam = 6 + azar.Next(0, 3);
                var punto = new BoxView
                {
                    Color = color,
                    CornerRadius = tam / 2,
                    WidthRequest = tam,
                    HeightRequest = tam,
                    InputTransparent = true,
                };
                if (contenedor is AbsoluteLayout)
                    AbsoluteLayout.SetLayoutBounds(punto, new Rect(centro.X - tam / 2, centro.Y - tam / 2, tam, tam));
                else
                {
                    punto.HorizontalOptions = LayoutOptions.Start;
                    punto.VerticalOptions = LayoutOptions.Start;
                    punto.Margin = new Thickness(centro.X - tam / 2, centro.Y - tam / 2, 0, 0);
                }
                contenedor.Add(punto);
                creadas.Add(punto);
                tareas.Add(Task.WhenAll(
                    punto.TranslateTo(Math.Cos(angulo) * distancia, Math.Sin(angulo) * distancia, DuracionMs, Easing.CubicOut),
                    punto.FadeTo(0, DuracionMs, Easing.CubicIn),
                    punto.ScaleTo(0, DuracionMs, Easing.CubicIn)));
            }
            await Task.WhenAll(tareas);
        }
        finally
        {
            foreach (var p in creadas)
            {
                p.CancelAnimations();
                contenedor.Remove(p);
            }
        }
    }
}
