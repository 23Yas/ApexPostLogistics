using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Courier;

public partial class App : Application
{
	public App(IServiceProvider services)
	{
		InitializeComponent();
		var sync = services.GetRequiredService<Services.OfflineSyncService>();
		sync.StartConnectivityListener();

		// On 401: token already cleared by the client; navigate back to Login.
		services.GetRequiredService<Services.ApexApiClient>().Unauthorized += () =>
			MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}