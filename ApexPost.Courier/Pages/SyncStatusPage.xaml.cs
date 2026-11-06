using ApexPost.Courier.Services;

namespace ApexPost.Courier.Pages;

public partial class SyncStatusPage : ContentPage
{
    private readonly OfflineSyncService _syncService;

    public SyncStatusPage(OfflineSyncService syncService)
    {
        InitializeComponent();
        _syncService = syncService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LastSyncLabel.Text = $"Last sync: {(_syncService.LastSyncTimeUtc?.ToString("u") ?? "never")}";
        CircuitLabel.Text = $"Circuit state: {_syncService.CircuitState}";
        FailuresLabel.Text = $"Last failures: {_syncService.LastFailureCount}";
        OfflineBanner.IsVisible = _syncService.CircuitState == "Open";
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//pending");
}
