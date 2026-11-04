using CineList.Helpers;

namespace CineList.Controls;

// Fila de 5 estrellas. Si es editable, tocar una estrella asigna la calificación;
// tocar la estrella ya activa la deja en 0 (sin calificar).
public class EstrellasView : HorizontalStackLayout
{
    public static readonly BindableProperty ValorProperty = BindableProperty.Create(
        nameof(Valor), typeof(int), typeof(EstrellasView), 0, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((EstrellasView)b).Pintar());

    public static readonly BindableProperty EsEditableProperty = BindableProperty.Create(
        nameof(EsEditable), typeof(bool), typeof(EstrellasView), false,
        propertyChanged: (b, _, nuevo) => ((EstrellasView)b).ConfigurarToques((bool)nuevo));

    public static readonly BindableProperty TamanoProperty = BindableProperty.Create(
        nameof(Tamano), typeof(double), typeof(EstrellasView), 14.0,
        propertyChanged: (b, _, _) => ((EstrellasView)b).Pintar());

    public int Valor
    {
        get => (int)GetValue(ValorProperty);
        set => SetValue(ValorProperty, value);
    }

    public bool EsEditable
    {
        get => (bool)GetValue(EsEditableProperty);
        set => SetValue(EsEditableProperty, value);
    }

    public double Tamano
    {
        get => (double)GetValue(TamanoProperty);
        set => SetValue(TamanoProperty, value);
    }

    private readonly Label[] _estrellas = new Label[5];
    private readonly TapGestureRecognizer[] _toques = new TapGestureRecognizer[5];

    public EstrellasView()
    {
        Spacing = 1;
        for (var i = 0; i < _estrellas.Length; i++)
        {
            var numero = i + 1;
            var estrella = new Label { Text = Iconos.Estrella, VerticalOptions = LayoutOptions.Center };
            estrella.SetAppThemeColor(Label.TextColorProperty,
                Recurso("AcentoClaro"), Recurso("AcentoOscuro"));
            var toque = new TapGestureRecognizer();
            toque.Tapped += async (_, _) => await Tocar(numero, estrella);
            _toques[i] = toque;
            _estrellas[i] = estrella;
            Children.Add(estrella);
        }
        Pintar();
    }

    // Solo las estrellas editables escuchan toques (en la lista no deben robar el tap de la celda)
    private void ConfigurarToques(bool editable)
    {
        for (var i = 0; i < _estrellas.Length; i++)
        {
            _estrellas[i].GestureRecognizers.Remove(_toques[i]);
            if (editable) _estrellas[i].GestureRecognizers.Add(_toques[i]);
        }
    }

    private async Task Tocar(int numero, Label estrella)
    {
        if (!EsEditable) return;
        Valor = Valor == numero ? 0 : numero;
        await Animaciones.Pop(estrella);
    }

    // Deja las estrellas en escala 0 antes de que el bloque aparezca (luego AnimarLlenado)
    public void PrepararLlenado()
    {
        if (Animaciones.ReducirMovimiento) return;
        foreach (var e in _estrellas) e.Scale = 0;
    }

    // Estado final sin animación (por si la entrada se interrumpió)
    public void MostrarLlenas()
    {
        foreach (var e in _estrellas) e.Scale = 1;
    }

    // Las estrellas aparecen una a una (0 -> 1.25 -> 1), separadas retrasoMs
    public async Task AnimarLlenado(uint retrasoMs = 60)
    {
        try
        {
            if (Animaciones.ReducirMovimiento) return;
            foreach (var e in _estrellas) e.Scale = 0;
            await Task.WhenAll(_estrellas.Select((e, i) => AparecerEstrella(e, (int)(i * retrasoMs))));
        }
        finally
        {
            MostrarLlenas(); // pase lo que pase, quedan visibles
        }
    }

    private static async Task AparecerEstrella(Label estrella, int retraso)
    {
        await Task.Delay(retraso);
        await estrella.ScaleTo(1.25, 150, Easing.CubicOut);
        await estrella.ScaleTo(1, 120, Easing.CubicOut);
    }

    private void Pintar()
    {
        for (var i = 0; i < _estrellas.Length; i++)
        {
            // Llena: fuente rellena; vacía: contorno con menos opacidad
            var llena = i < Valor;
            _estrellas[i].FontFamily = llena ? Iconos.FuenteLlena : "Iconos";
            _estrellas[i].Opacity = llena ? 1 : 0.45;
            _estrellas[i].FontSize = Tamano;
        }
    }

    private static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out var valor) == true && valor is Color c ? c : Colors.Gray;
}
