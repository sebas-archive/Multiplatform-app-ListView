namespace CineList.Controls;

// Panel de estado (vacío, error, sin resultados): aparece con fade cada vez que se hace visible.
public class PanelEstado : VerticalStackLayout
{
    protected override async void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName != nameof(IsVisible) || !IsVisible) return;
        if (Helpers.Animaciones.ReducirMovimiento) { Opacity = 1; return; }
        try
        {
            Opacity = 0;
            await this.FadeTo(1, 250, Easing.CubicOut);
        }
        catch (Exception)
        {
            Opacity = 1; // nunca dejar el panel invisible
        }
    }
}
