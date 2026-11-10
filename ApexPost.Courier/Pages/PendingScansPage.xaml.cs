using ApexPost.Courier.Offline;
using ApexPost.Courier.Services;

namespace ApexPost.Courier.Pages;

public partial class PendingScansPage : ContentPage
{
    private readonly IScanRepository _repository;
    private readonly OfflineSyncService _syncService;

    public PendingScansPage(IScanRepository repository, CourierDatabase database, OfflineSyncService syncService)
    {
        InitializeComponent();
        _repository = repository;
        _syncService = syncService;

        // Live pending count: the Couchbase change listener fires on every local
        // document change (new scan, sync status update, delete).
        database.RegisterChangeListener(_ => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync()));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var pending = await _repository.GetPendingScansAsync();
        PendingCountLabel.Text = $"Pending scans: {pending.Count}";
        PendingList.ItemsSource = pending;
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        var summary = await _syncService.SyncAsync();
        await DisplayAlert("Sync complete",
            $"Synced: {summary.Synced}, Duplicates: {summary.DuplicatesSkipped}, Failed: {summary.Failed}", "OK");
        await RefreshAsync();
    }
}
