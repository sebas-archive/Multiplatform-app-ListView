namespace CineList;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("detalle", typeof(Views.DetallePage));
		Routing.RegisterRoute("formulario", typeof(Views.FormularioPage));
	}
}
