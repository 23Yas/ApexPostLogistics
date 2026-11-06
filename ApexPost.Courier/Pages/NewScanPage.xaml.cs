using ApexPost.Courier.Offline;
using ApexPost.Courier.Services;

namespace ApexPost.Courier.Pages;

public partial class NewScanPage : ContentPage
{
    private readonly IScanRepository _repository;

    public NewScanPage(IScanRepository repository)
    {
        InitializeComponent();
        _repository = repository;
        ScanTypePicker.ItemsSource = new[] { "Pickup", "InTransit", "Delivered", "Failed" };
        ScanTypePicker.SelectedIndex = 0;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TrackingEntry.Text) || string.IsNullOrWhiteSpace(LocationEntry.Text))
        {
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = "Tracking number and location are required.";
            return;
        }

        var scan = new ApexPost.Courier.Offline.ShipmentScan
        {
            TrackingNumber = TrackingEntry.Text.Trim(),
            ScanType = Enum.Parse<ApexPost.Courier.Offline.ScanType>(ScanTypePicker.SelectedItem?.ToString() ?? "Pickup"),
            CourierId = CourierEntry.Text?.Trim() ?? "courier-001",
            Location = LocationEntry.Text.Trim(),
            ScannedAtUtc = DateTimeOffset.UtcNow,
            Notes = NotesEntry.Text ?? string.Empty,
            SyncStatus = ApexPost.Courier.Offline.SyncStatus.Pending
        };

        await _repository.CreateAsync(scan);
        StatusLabel.TextColor = Colors.Green;
        StatusLabel.Text = $"Saved locally. Scan ID: {scan.Id}";
    }
}
