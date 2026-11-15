namespace ApexPost.Tests;

using ApexPost.Courier.Offline;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

public sealed class OutboxServiceTests
{
    private const string Tracking = "APX-12345678";

    private static ShipmentScan NewScan() => new()
    {
        TrackingNumber = Tracking,
        ScanType = ScanType.Pickup,
        CourierId = "courier-001",
        Location = "Depot A",
        ScannedAtUtc = DateTimeOffset.UtcNow
    };

    private static (OutboxService Service, Mock<IScanRepository> Repo, Mock<IShipmentUploader> Uploader) Build(
        List<ShipmentScan>? pending = null,
        UploadResult? uploadResult = null,
        Exception? uploadException = null)
    {
        var repo = new Mock<IScanRepository>();
        repo.Setup(r => r.GetPendingScansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending ?? new List<ShipmentScan>());

        var uploader = new Mock<IShipmentUploader>();
        if (uploadException is not null)
        {
            uploader.Setup(u => u.UploadScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(uploadException);
        }
        else
        {
            uploader.Setup(u => u.UploadScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadResult ?? new UploadResult { Success = true });
        }

        return (new OutboxService(repo.Object, uploader.Object, NullLogger<OutboxService>.Instance), repo, uploader);
    }

    [Fact]
    public async Task SuccessfulUpload_MarksScanSynced()
    {
        var scan = NewScan();
        var (service, repo, _) = Build(new List<ShipmentScan> { scan });

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Synced);
        Assert.Equal(SyncStatus.Synced, scan.SyncStatus);
        repo.Verify(r => r.UpdateAsync(It.Is<ShipmentScan>(s => s.SyncStatus == SyncStatus.Synced), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransientFailure_LeavesScanPending()
    {
        var scan = NewScan();
        var (service, repo, _) = Build(new List<ShipmentScan> { scan },
            new UploadResult { Success = false, ErrorMessage = "no network", IsTransient = true });

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Deferred);
        Assert.Equal(0, summary.Failed);
        Assert.Equal(SyncStatus.Pending, scan.SyncStatus);
        repo.Verify(r => r.UpdateAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PermanentRejection_MarksScanFailed()
    {
        var scan = NewScan();
        var (service, repo, _) = Build(new List<ShipmentScan> { scan },
            new UploadResult { Success = false, ErrorMessage = "validation", IsTransient = false });

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Failed);
        Assert.Equal(SyncStatus.Failed, scan.SyncStatus);
        repo.Verify(r => r.UpdateAsync(It.Is<ShipmentScan>(s => s.SyncStatus == SyncStatus.Failed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploaderException_LeavesScanPending()
    {
        var scan = NewScan();
        var (service, _, uploader) = Build(new List<ShipmentScan> { scan },
            uploadException: new HttpRequestException("connection reset"));

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(1, summary.Deferred);
        Assert.Equal(SyncStatus.Pending, scan.SyncStatus);
        uploader.Verify(u => u.UploadScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentSync_IsSingleFlight()
    {
        var scan = NewScan();
        var repo = new Mock<IScanRepository>();
        repo.Setup(r => r.GetPendingScansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShipmentScan> { scan });

        var uploader = new Mock<IShipmentUploader>();
        uploader.Setup(u => u.UploadScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()))
            .Returns((ShipmentScan _, CancellationToken ct) => SlowUpload(ct));

        var service = new OutboxService(repo.Object, uploader.Object, NullLogger<OutboxService>.Instance);

        var first = service.SyncPendingScansAsync();
        var second = await service.SyncPendingScansAsync(); // overlaps the first run

        Assert.Equal(0, second.TotalPending); // skipped: no work done twice
        var summary = await first;
        Assert.Equal(1, summary.Synced);
        uploader.Verify(u => u.UploadScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Once);

        static async Task<UploadResult> SlowUpload(CancellationToken ct)
        {
            await Task.Delay(200, ct);
            return new UploadResult { Success = true };
        }
    }

    [Fact]
    public async Task NoPendingScans_ReturnsEmptySummary_WithoutCallingUploader()
    {
        var (service, _, uploader) = Build(new List<ShipmentScan>());

        var summary = await service.SyncPendingScansAsync();

        Assert.Equal(0, summary.TotalPending);
        uploader.Verify(u => u.UploadScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
