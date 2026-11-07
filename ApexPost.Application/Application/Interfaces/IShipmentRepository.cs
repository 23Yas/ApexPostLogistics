using ApexPost.Core.Domain;

namespace ApexPost.Core.Application.Interfaces;

public interface IShipmentRepository
{
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Shipment>> GetPagedAsync(int page, int pageSize, string? status = null, string? city = null, CancellationToken cancellationToken = default);
    Task<Shipment> CreateAsync(Shipment shipment, CancellationToken cancellationToken = default);
    Task<Shipment?> UpdateAsync(string trackingNumber, Shipment shipment, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(string trackingNumber, string newStatus, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string trackingNumber, CancellationToken cancellationToken = default);

    Task<ShipmentScan?> GetScanByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ShipmentScan>> GetScansByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<ShipmentScan?> GetLatestScanAsync(string trackingNumber, CancellationToken cancellationToken = default);
}
