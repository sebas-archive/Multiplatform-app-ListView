using CineList.Helpers;
using CineList.Services;

namespace CineList.Views;

public partial class PeliculasPage : ContentPage
{
	private readonly TemaService _tema;

	public PeliculasPage(TemaService tema)
	{
		InitializeComponent();
		_tema = tema;
		ActualizarIcono();
	}

	private void ActualizarIcono()
	{
		// En modo oscuro se ofrece el sol (pasar a claro); en modo claro, la luna
		BotonTema.Text = _tema.TemaActual == AppTheme.Dark ? Iconos.ModoClaro : Iconos.ModoOscuro;
		Descripcion.Text = _tema.TemaActual == AppTheme.Dark
			? "Modo oscuro activo. Toca el icono para cambiar a claro."
			: "Modo claro activo. Toca el icono para cambiar a oscuro.";
	}

	private async void OnAlternarTema(object? sender, EventArgs e)
	{
		var giro = Animaciones.ReducirMovimiento ? Task.CompletedTask : BotonTema.RotateTo(BotonTema.Rotation + 180, 350, Easing.CubicOut);
		await _tema.AlternarAsync(Contenido);
		ActualizarIcono();
		await giro;
		BotonTema.Rotation %= 360;
	}
}
