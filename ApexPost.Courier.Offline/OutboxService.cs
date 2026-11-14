using Microsoft.Extensions.Logging;

namespace ApexPost.Courier.Offline;

public sealed class OutboxService
{
    private readonly IScanRepository _scanRepository;
    private readonly IShipmentUploader _shipmentUploader;
    private readonly ILogger<OutboxService> _logger;
    private int _syncInProgress;

    public OutboxService(
        IScanRepository scanRepository,
        IShipmentUploader shipmentUploader,
        ILogger<OutboxService> logger)
    {
        _scanRepository = scanRepository;
        _shipmentUploader = shipmentUploader;
        _logger = logger;
    }

    /// <summary>
    /// Uploads every Pending scan once. Single-flight: a second call while a sync is
    /// running returns an empty summary instead of overlapping uploads.
    /// Transient failures (no network, timeout, 5xx) leave the scan Pending so a courier
    /// syncing in a dead zone does not lose scans; only real rejections mark it Failed.
    /// </summary>
    public async Task<SyncSummary> SyncPendingScansAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _syncInProgress, 1) == 1)
        {
            _logger?.LogWarning("Sync already in progress - skipping overlapping run");
            return new SyncSummary();
        }

        try
        {
            var pendingScans = await _scanRepository.GetPendingScansAsync(cancellationToken);
            var summary = new SyncSummary
            {
                TotalPending = pendingScans.Count
            };

            _logger?.LogInformation("Starting sync of {Count} pending scans", pendingScans.Count);

            foreach (var scan in pendingScans)
            {
                UploadResult result;
                try
                {
                    result = await _shipmentUploader.UploadScanAsync(scan, cancellationToken);
                }
                catch (Exception ex)
                {
                    // An exception escaping the uploader is not proof of rejection - keep
                    // the scan Pending so it is retried on the next sync.
                    _logger?.LogWarning(ex, "Transient failure during sync of scan {ScanId}; leaving Pending", scan.Id);
                    summary.Deferred++;
                    continue;
                }

                if (result.Success)
                {
                    scan.SyncStatus = SyncStatus.Synced;
                    await _scanRepository.UpdateAsync(scan, cancellationToken);
                    summary.Synced++;
                    _logger?.LogDebug("Scan {ScanId} synced successfully", scan.Id);
                }
                else if (result.IsTransient)
                {
                    // Network/server problem: scan stays Pending and is retried later.
                    summary.Deferred++;
                    _logger?.LogWarning("Scan {ScanId} deferred (transient): {Error}", scan.Id, result.ErrorMessage);
                }
                else
                {
                    scan.SyncStatus = SyncStatus.Failed;
                    await _scanRepository.UpdateAsync(scan, cancellationToken);
                    summary.Failed++;
                    _logger?.LogWarning("Scan {ScanId} sync failed permanently: {Error}", scan.Id, result.ErrorMessage);
                }
            }

            _logger?.LogInformation("Sync complete: {Synced} synced, {Failed} failed, {Deferred} deferred",
                summary.Synced, summary.Failed, summary.Deferred);
            return summary;
        }
        finally
        {
            Interlocked.Exchange(ref _syncInProgress, 0);
        }
    }
}

public sealed class SyncSummary
{
    public int TotalPending { get; init; }
    public int Synced { get; set; }
    public int Failed { get; set; }

    /// <summary>Scans left Pending because of a transient (network/server) failure.</summary>
    public int Deferred { get; set; }
}
