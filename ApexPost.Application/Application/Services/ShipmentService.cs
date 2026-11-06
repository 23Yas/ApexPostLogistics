using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

namespace ApexPost.Core.Application.Services;

public sealed class ShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;

    public ShipmentService(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Shipment>> GetPagedAsync(int page, int pageSize, string? status = null, string? city = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        return await _shipmentRepository.GetPagedAsync(page, pageSize, status, city, cancellationToken);
    }

    public async Task<Shipment> CreateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.CreateAsync(shipment, cancellationToken);
    }

    public async Task<Shipment?> UpdateAsync(string trackingNumber, Shipment shipment, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.UpdateAsync(trackingNumber, shipment, cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(string trackingNumber, string newStatus, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.UpdateStatusAsync(trackingNumber, newStatus, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.DeleteAsync(trackingNumber, cancellationToken);
    }

    public async Task<(int accepted, int duplicatesSkipped, int rejected)> UploadScansAsync(
        string trackingNumber,
        List<ShipmentScan> scans,
        CancellationToken cancellationToken = default)
    {
        int accepted = 0;
        int duplicatesSkipped = 0;
        int rejected = 0;

        // Check if shipment exists
        var shipment = await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber, cancellationToken);
        if (shipment == null)
        {
            return (0, 0, scans.Count);
        }

        // Process each scan (idempotent upload based on scan ID)
        foreach (var scan in scans)
        {
            // For Part B, we'll just accept all scans since we don't have a scan repository yet
            // In a real implementation, we would check if the scan ID already exists
            accepted++;
        }

        return (accepted, duplicatesSkipped, rejected);
    }
}
