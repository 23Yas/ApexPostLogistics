namespace ApexPost.Tests;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Moq;

/// <summary>
/// Unit tests for ShipmentService business rules. IShipmentRepository is mocked with Moq:
/// these tests prove the service's decisions and repository calls, not SQL behaviour
/// (that is covered by ApexPost.IntegrationTests against a real database).
/// </summary>
public sealed class ShipmentServiceTests
{
    private const string Tracking = "APX-12345678";

    private static Shipment NewShipment(string status = "Created") => new()
    {
        TrackingReference = Tracking,
        Status = status
    };

    private static ShipmentScan NewScan(ScanType type = ScanType.Pickup, string? tracking = null) => new()
    {
        TrackingNumber = tracking ?? Tracking,
        ScanType = type,
        CourierId = "courier-001",
        Location = "Depot A",
        ScannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
    };

    [Fact]
    public async Task GetPagedAsync_PageZero_ThrowsBadRequest()
    {
        var service = new ShipmentService(new Mock<IShipmentRepository>().Object);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.GetPagedAsync(0, 20));
        Assert.Contains("page", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task GetPagedAsync_PageSizeOutOfRange_ThrowsBadRequest(int pageSize)
    {
        var service = new ShipmentService(new Mock<IShipmentRepository>().Object);

        await Assert.ThrowsAsync<BadRequestException>(() => service.GetPagedAsync(1, pageSize));
    }

    [Fact]
    public async Task GetPagedAsync_ValidArgs_DelegatesToRepository()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetPagedAsync(2, 10, "Created", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Shipment> { NewShipment() });
        var service = new ShipmentService(repo.Object);

        var result = await service.GetPagedAsync(2, 10, "Created");

        Assert.Single(result);
        repo.Verify(r => r.GetPagedAsync(2, 10, "Created", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateTracking_ThrowsConflict_AndNeverPersists()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment());
        var service = new ShipmentService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(NewShipment()));

        repo.Verify(r => r.CreateAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NewTracking_PersistsAndReturns()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);
        repo.Setup(r => r.CreateAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment s, CancellationToken _) => s);
        var service = new ShipmentService(repo.Object);

        var created = await service.CreateAsync(NewShipment());

        Assert.Equal(Tracking, created.TrackingReference);
        repo.Verify(r => r.CreateAsync(It.Is<Shipment>(s => s.TrackingReference == Tracking), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsConflict_AndDoesNotPersist()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("Created"));
        var service = new ShipmentService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateStatusAsync(Tracking, "Delivered"));

        repo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_PersistsAndReturnsUpdated()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("Created"));
        repo.Setup(r => r.UpdateStatusAsync(Tracking, "PickedUp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new ShipmentService(repo.Object);

        var updated = await service.UpdateStatusAsync(Tracking, "PickedUp");

        Assert.Equal("PickedUp", updated.Status);
        repo.Verify(r => r.UpdateStatusAsync(Tracking, "PickedUp", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_MissingShipment_ThrowsNotFound()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);
        var service = new ShipmentService(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateStatusAsync(Tracking, "PickedUp"));
    }

    [Theory]
    [InlineData("PickedUp")]
    [InlineData("Delivered")]
    public async Task DeleteAsync_NonCreatedStatus_ThrowsConflict(string status)
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment(status));
        var service = new ShipmentService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(Tracking));

        repo.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CreatedStatus_DelegatesToRepository()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("Created"));
        repo.Setup(r => r.DeleteAsync(Tracking, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new ShipmentService(repo.Object);

        await service.DeleteAsync(Tracking);

        repo.Verify(r => r.DeleteAsync(Tracking, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadScansAsync_DuplicateScanId_SkippedNotRejected()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("PickedUp"));
        var existing = NewScan(ScanType.InTransit);
        repo.Setup(r => r.GetScanByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var service = new ShipmentService(repo.Object);

        var outcome = await service.UploadScansAsync(Tracking, new[] { existing });

        Assert.Equal(0, outcome.Accepted);
        Assert.Equal(1, outcome.DuplicatesSkipped);
        Assert.Equal(0, outcome.Rejected);
        repo.Verify(r => r.AddScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadScansAsync_TrackingMismatch_RejectedWithReason()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("Created"));
        repo.Setup(r => r.GetScanByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentScan?)null);
        var service = new ShipmentService(repo.Object);
        var wrongTracking = NewScan(tracking: "APX-99999999");

        var outcome = await service.UploadScansAsync(Tracking, new[] { wrongTracking });

        Assert.Equal(1, outcome.Rejected);
        Assert.Equal("tracking_number_mismatch", outcome.Rejections.Single().Reason);
        repo.Verify(r => r.AddScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadScansAsync_FutureScan_RejectedWithReason()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("Created"));
        repo.Setup(r => r.GetScanByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentScan?)null);
        var service = new ShipmentService(repo.Object);
        var future = NewScan();
        // ScannedAtUtc is init-only on the entity; build a future-dated scan via object initializer.
        future = new ShipmentScan
        {
            TrackingNumber = Tracking,
            ScanType = ScanType.Pickup,
            CourierId = "courier-001",
            Location = "Depot A",
            ScannedAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        var outcome = await service.UploadScansAsync(Tracking, new[] { future });

        Assert.Equal(1, outcome.Rejected);
        Assert.Equal("scan_time_in_future", outcome.Rejections.Single().Reason);
    }

    [Fact]
    public async Task UploadScansAsync_ValidPickup_AcceptedAndPersisted()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewShipment("Created"));
        repo.Setup(r => r.GetScanByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentScan?)null);
        repo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new ShipmentService(repo.Object);

        var outcome = await service.UploadScansAsync(Tracking, new[] { NewScan() });

        Assert.Equal(1, outcome.Accepted);
        repo.Verify(r => r.AddScanAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateStatusAsync(Tracking, "PickedUp", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadScansAsync_MissingShipment_ThrowsNotFound()
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByTrackingNumberAsync(Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);
        var service = new ShipmentService(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UploadScansAsync(Tracking, new[] { NewScan() }));
    }
}
