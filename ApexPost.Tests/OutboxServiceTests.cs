namespace ApexPost.Tests;

using ApexPost.Courier.Offline;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>In-memory IScanRepository for OutboxService tests.</summary>
public sealed class FakeScanRepository : IScanRepository
{
    public List<ShipmentScan> Scans { get; } = new();

    public Task<ShipmentScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Scans.FirstOrDefault(s => s.Id == id));

    public Task CreateAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        Scans.Add(scan);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        var index = Scans.FindIndex(s => s.Id == scan.Id);
        if (index >= 0) Scans[index] = scan;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Scans.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }

    public Task<List<ShipmentScan>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Scans.ToList());

    public Task<List<ShipmentScan>> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(Scans.Where(s => s.TrackingNumber == trackingNumber).ToList());

    public Task<List<ShipmentScan>> GetPendingScansAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Scans.Where(s => s.SyncStatus == SyncStatus.Pending).OrderBy(s => s.ScannedAtUtc).ToList());

    public Task<Dictionary<ScanType, int>> GetScanCountsByTypeAsync(string courierId, CancellationToken cancellationToken = default)
        => Task.FromResult(Scans.Where(s => s.CourierId == courierId)
            .GroupBy(s => s.ScanType).ToDictionary(g => g.Key, g => g.Count()));
}

/// <summary>Uploader whose results are scripted per call so tests control success/transient/permanent.</summary>
public sealed class ScriptableUploader : IShipmentUploader
{
    private readonly Queue<Func<ShipmentScan, UploadResult>> _results = new();
    private readonly Queue<Exception> _exceptions = new();
    public int CallCount { get; private set; }

    public void Enqueue(UploadResult result) => _results.Enqueue(_ => result);
    public void EnqueueException(Exception ex) => _exceptions.Enqueue(ex);

    public Task<UploadResult> UploadScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (_exceptions.Count > 0) throw _exceptions.Dequeue();
        return Task.FromResult(_results.Count > 0 ? _results.Dequeue()(scan) : new UploadResult { Success = true });
    }
}

public sealed class OutboxServiceTests
{
    private static ShipmentScan NewScan() => new()
    {
        TrackingNumber = "APX-12345678",
        ScanType = ScanType.Pickup,
        CourierId = "courier-001",
        Location = "Depot A",
        ScannedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task SuccessfulUpload_MarksScanSynced()
    {
        var repo = new FakeScanRepository();
        var scan = NewScan();
        await repo.CreateAsync(scan);
        var uploader = new ScriptableUploader();
        var service = new OutboxService(repo, uploader, NullLogger<OutboxService>.Instance);

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Synced);
        Assert.Equal(SyncStatus.Synced, repo.Scans.Single().SyncStatus);
    }

    [Fact]
    public async Task TransientFailure_LeavesScanPending()
    {
        var repo = new FakeScanRepository();
        var scan = NewScan();
        await repo.CreateAsync(scan);
        var uploader = new ScriptableUploader();
        uploader.Enqueue(new UploadResult { Success = false, ErrorMessage = "no network", IsTransient = true });
        var service = new OutboxService(repo, uploader, NullLogger<OutboxService>.Instance);

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Deferred);
        Assert.Equal(0, summary.Failed);
        Assert.Equal(SyncStatus.Pending, repo.Scans.Single().SyncStatus);
        // Still returned by the pending query, so a later sync retries it.
        Assert.Single(await repo.GetPendingScansAsync());
    }

    [Fact]
    public async Task PermanentRejection_MarksScanFailed()
    {
        var repo = new FakeScanRepository();
        var scan = NewScan();
        await repo.CreateAsync(scan);
        var uploader = new ScriptableUploader();
        uploader.Enqueue(new UploadResult { Success = false, ErrorMessage = "validation", IsTransient = false });
        var service = new OutboxService(repo, uploader, NullLogger<OutboxService>.Instance);

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Failed);
        Assert.Equal(SyncStatus.Failed, repo.Scans.Single().SyncStatus);
    }

    [Fact]
    public async Task UploaderException_LeavesScanPending()
    {
        var repo = new FakeScanRepository();
        var scan = NewScan();
        await repo.CreateAsync(scan);
        var uploader = new ScriptableUploader();
        uploader.EnqueueException(new HttpRequestException("connection reset"));
        var service = new OutboxService(repo, uploader, NullLogger<OutboxService>.Instance);

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Deferred);
        Assert.Equal(SyncStatus.Pending, repo.Scans.Single().SyncStatus);
    }

    [Fact]
    public async Task ConcurrentSync_IsSingleFlight()
    {
        var repo = new FakeScanRepository();
        await repo.CreateAsync(NewScan());
        var uploader = new SlowUploader();
        var service = new OutboxService(repo, uploader, NullLogger<OutboxService>.Instance);

        var first = service.SyncPendingScansAsync();
        var second = await service.SyncPendingScansAsync(); // overlaps the first run

        Assert.Equal(0, second.TotalPending); // skipped: no work done twice
        var summary = await first;
        Assert.Equal(1, summary.Synced);
        Assert.Equal(1, uploader.CallCount);
    }

    private sealed class SlowUploader : IShipmentUploader
    {
        public int CallCount;
        public async Task<UploadResult> UploadScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CallCount);
            await Task.Delay(200, cancellationToken);
            return new UploadResult { Success = true };
        }
    }
}
