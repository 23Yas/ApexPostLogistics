namespace ApexPost.Courier.Offline;

public interface IShipmentUploader
{
    Task<UploadResult> UploadScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default);
}

public sealed class UploadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
