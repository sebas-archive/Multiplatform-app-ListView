using CineList.Services;

namespace CineList;

public partial class App : Application
{
	public App(TemaService tema)
	{
		InitializeComponent();
		tema.Inicializar();
		// La barra de estado de Android acompaña al tema (fondo e íconos claros/oscuros)
		RequestedThemeChanged += (_, e) => BarraEstado.Aplicar(e.RequestedTheme);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
