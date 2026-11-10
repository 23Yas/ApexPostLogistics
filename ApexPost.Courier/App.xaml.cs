using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Courier;

public partial class App : Application
{
	public App(IServiceProvider services)
	{
		InitializeComponent();
		services.GetRequiredService<Services.OfflineSyncService>().StartConnectivityListener();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}