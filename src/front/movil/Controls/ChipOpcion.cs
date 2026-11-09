using CineList.Helpers;

namespace CineList.Controls;

// Chip del panel de filtros (géneros y décadas): activo según Activo; al tocarlo hace "pop" (1 -> 1.15 -> 1).
public class ChipOpcion : ChipBase
{
    public static readonly BindableProperty ActivoProperty = BindableProperty.Create(
        nameof(Activo), typeof(bool), typeof(ChipOpcion), false,
        propertyChanged: (b, _, nuevo) => ((ChipOpcion)b).Marcar((bool)nuevo, animar: true));

    public bool Activo
    {
        get => (bool)GetValue(ActivoProperty);
        set => SetValue(ActivoProperty, value);
    }

    public ChipOpcion()
    {
        Marcar(false, animar: false);
        Clicked += async (_, _) =>
        {
            try { await Animaciones.Pop(this, 1.15); }
            finally { Scale = 1; }
        };
    }
}
