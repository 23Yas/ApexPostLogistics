using Microsoft.Extensions.Logging;

namespace ApexPost.Courier.Offline;

public sealed class OutboxService
{
    private readonly IScanRepository _scanRepository;
    private readonly IShipmentUploader _shipmentUploader;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        IScanRepository scanRepository,
        IShipmentUploader shipmentUploader,
        ILogger<OutboxService> logger)
    {
        _scanRepository = scanRepository;
        _shipmentUploader = shipmentUploader;
        _logger = logger;
    }

    public async Task<SyncSummary> SyncPendingScansAsync(CancellationToken cancellationToken = default)
    {
        var pendingScans = await _scanRepository.GetPendingScansAsync(cancellationToken);
        var summary = new SyncSummary
        {
            TotalPending = pendingScans.Count
        };

        _logger?.LogInformation("Starting sync of {Count} pending scans", pendingScans.Count);

        foreach (var scan in pendingScans)
        {
            try
            {
                var result = await _shipmentUploader.UploadScanAsync(scan, cancellationToken);

                if (result.Success)
                {
                    scan.SyncStatus = SyncStatus.Synced;
                    await _scanRepository.UpdateAsync(scan, cancellationToken);
                    summary.Synced++;
                    _logger?.LogDebug("Scan {ScanId} synced successfully", scan.Id);
                }
                else
                {
                    scan.SyncStatus = SyncStatus.Failed;
                    await _scanRepository.UpdateAsync(scan, cancellationToken);
                    summary.Failed++;
                    _logger?.LogWarning("Scan {ScanId} sync failed: {Error}", scan.Id, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                scan.SyncStatus = SyncStatus.Failed;
                await _scanRepository.UpdateAsync(scan, cancellationToken);
                summary.Failed++;
                _logger?.LogError(ex, "Exception during sync of scan {ScanId}", scan.Id);
            }
        }

        _logger?.LogInformation("Sync complete: {Synced} synced, {Failed} failed", summary.Synced, summary.Failed);
        return summary;
    }
}

public sealed class SyncSummary
{
    public int TotalPending { get; init; }
    public int Synced { get; set; }
    public int Failed { get; set; }
}
