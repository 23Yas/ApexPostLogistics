namespace ApexPost.Courier.Offline;

public interface IShipmentUploader
{
    Task<UploadResult> UploadScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default);
}

public sealed class UploadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// True when the failure is transient (no network, timeout, 5xx, auth-expired) and the
    /// scan should stay Pending for a later retry. False means the server rejected the scan
    /// for good, so it is marked Failed.
    /// </summary>
    public bool IsTransient { get; init; }
}
