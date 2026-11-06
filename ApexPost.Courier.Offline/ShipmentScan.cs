namespace ApexPost.Courier.Offline;

public enum ScanType
{
    Pickup,
    InTransit,
    Delivered,
    Failed
}

public enum SyncStatus
{
    Pending,
    Synced,
    Failed
}

public sealed class ShipmentScan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TrackingNumber { get; init; } = string.Empty;
    public ScanType ScanType { get; init; }
    public string CourierId { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTimeOffset ScannedAtUtc { get; init; }
    public string Notes { get; init; } = string.Empty;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
}
