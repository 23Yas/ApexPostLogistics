using Microsoft.Extensions.Logging;

namespace ApexPost.Courier.Offline;

public sealed class FakeShipmentUploader : IShipmentUploader
{
    private readonly ILogger<FakeShipmentUploader> _logger;

    public FakeShipmentUploader(ILogger<FakeShipmentUploader> logger)
    {
        _logger = logger;
    }

    public Task<UploadResult> UploadScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        // Simulate API call with random success/failure for testing
        var random = new Random();
        var shouldSucceed = random.NextDouble() > 0.2; // 80% success rate

        if (shouldSucceed)
        {
            _logger?.LogInformation("Fake upload succeeded for scan {ScanId} tracking {TrackingNumber}", scan.Id, scan.TrackingNumber);
            return Task.FromResult(new UploadResult { Success = true });
        }
        else
        {
            var error = "Simulated API failure";
            _logger?.LogWarning("Fake upload failed for scan {ScanId}: {Error}", scan.Id, error);
            return Task.FromResult(new UploadResult { Success = false, ErrorMessage = error });
        }
    }
}
