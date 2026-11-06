namespace ApexPost.Courier.Offline;

public interface IScanRepository
{
    Task<ShipmentScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(ShipmentScan scan, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShipmentScan scan, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ShipmentScan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ShipmentScan>> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<List<ShipmentScan>> GetPendingScansAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<ScanType, int>> GetScanCountsByTypeAsync(string courierId, CancellationToken cancellationToken = default);
}
