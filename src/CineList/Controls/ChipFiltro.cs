using CineList.Core.Lista;

namespace CineList.Controls;

// Chip de filtro (Todas / Vistas / Pendientes). Está activo cuando Seleccionado == Valor.
public class ChipFiltro : ChipBase
{
    public static readonly BindableProperty ValorProperty = BindableProperty.Create(
        nameof(Valor), typeof(FiltroEstado), typeof(ChipFiltro), FiltroEstado.Todas,
        propertyChanged: (b, _, _) => ((ChipFiltro)b).Actualizar(animar: false));

    public static readonly BindableProperty SeleccionadoProperty = BindableProperty.Create(
        nameof(Seleccionado), typeof(FiltroEstado), typeof(ChipFiltro), FiltroEstado.Todas,
        propertyChanged: (b, _, _) => ((ChipFiltro)b).Actualizar(animar: true));

    public FiltroEstado Valor
    {
        get => (FiltroEstado)GetValue(ValorProperty);
        set => SetValue(ValorProperty, value);
    }

    public FiltroEstado Seleccionado
    {
        get => (FiltroEstado)GetValue(SeleccionadoProperty);
        set => SetValue(SeleccionadoProperty, value);
    }

    public ChipFiltro()
    {
        Actualizar(animar: false);
    }

    private void Actualizar(bool animar) => Marcar(Valor == Seleccionado, animar);
}
