using CineList.Services;

namespace CineList;

public partial class App : Application
{
	public App(TemaService tema)
	{
		InitializeComponent();
		tema.Inicializar();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
