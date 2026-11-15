namespace ApexPost.Tests;

using ApexPost.Courier.Offline;
using Couchbase.Lite;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Exercises the real Couchbase Lite repository against a throwaway database in a temp
/// directory (Couchbase Lite is embedded - no server needed).
/// </summary>
public sealed class CouchbaseScanRepositoryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"apexpost-test-{Guid.NewGuid():N}");
    private readonly CourierDatabase _database;
    private readonly CouchbaseScanRepository _repository;

    public CouchbaseScanRepositoryTests()
    {
        Directory.CreateDirectory(_tempDir);
        _database = new CourierDatabase($"test_{Guid.NewGuid():N}",
            NullLogger<CourierDatabase>.Instance,
            new DatabaseConfiguration { Directory = _tempDir });
        _database.CreateIndexes();
        _repository = new CouchbaseScanRepository(_database, NullLogger<CouchbaseScanRepository>.Instance);
    }

    private static ShipmentScan NewScan(string tracking = "APX-12345678", DateTimeOffset? at = null) => new()
    {
        TrackingNumber = tracking,
        ScanType = ScanType.Pickup,
        CourierId = "courier-001",
        Location = "Depot A",
        ScannedAtUtc = at ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Notes = ""
    };

    [Fact]
    public async Task Create_ThenGetById_RoundTripsAllFields()
    {
        var scan = NewScan(at: new DateTimeOffset(2026, 11, 15, 10, 30, 0, TimeSpan.FromHours(5.5)));
        await _repository.CreateAsync(scan);

        var loaded = await _repository.GetByIdAsync(scan.Id);

        Assert.NotNull(loaded);
        Assert.Equal(scan.Id, loaded!.Id);
        Assert.Equal(scan.TrackingNumber, loaded.TrackingNumber);
        Assert.Equal(scan.ScanType, loaded.ScanType);
        Assert.Equal(scan.CourierId, loaded.CourierId);
        Assert.Equal(SyncStatus.Pending, loaded.SyncStatus);
        // The UTC offset must survive the round-trip, not just the instant.
        Assert.Equal(TimeSpan.FromHours(5.5), loaded.ScannedAtUtc.Offset);
        Assert.Equal(scan.ScannedAtUtc, loaded.ScannedAtUtc);
    }

    [Fact]
    public async Task Create_DuplicateId_Throws()
    {
        var scan = NewScan();
        await _repository.CreateAsync(scan);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.CreateAsync(scan));
    }

    [Fact]
    public async Task PendingQuery_ReturnsOnlyPending_OrderedByScanTime()
    {
        var older = NewScan(at: new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));
        var newer = NewScan(at: new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
        var synced = NewScan();
        await _repository.CreateAsync(older);
        await _repository.CreateAsync(newer);
        await _repository.CreateAsync(synced);
        synced.SyncStatus = SyncStatus.Synced;
        await _repository.UpdateAsync(synced);

        var pending = await _repository.GetPendingScansAsync();

        Assert.Equal(2, pending.Count);
        Assert.Equal(new[] { older.Id, newer.Id }, pending.Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task Update_ChangesSyncStatus_AndGetByTrackingNumber_Filters()
    {
        var scan = NewScan("APX-87654321");
        var other = NewScan("APX-11111111");
        await _repository.CreateAsync(scan);
        await _repository.CreateAsync(other);

        scan.SyncStatus = SyncStatus.Synced;
        await _repository.UpdateAsync(scan);

        var forTracking = await _repository.GetByTrackingNumberAsync("APX-87654321");
        Assert.Single(forTracking);
        Assert.Equal(SyncStatus.Synced, forTracking[0].SyncStatus);

        var pending = await _repository.GetPendingScansAsync();
        Assert.Single(pending);
        Assert.Equal(other.Id, pending[0].Id);
    }

    public void Dispose()
    {
        _database.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }
}
