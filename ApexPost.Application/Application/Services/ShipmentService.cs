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

    public async Task<IReadOnlyCollection<ShipmentScan>> GetScansAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.GetScansByTrackingNumberAsync(trackingNumber, cancellationToken);
    }

    public async Task<ShipmentScan?> GetLatestScanAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.GetLatestScanAsync(trackingNumber, cancellationToken);
    }

    /// <summary>
    /// Idempotent bulk upload of offline scans. Each scan carries its client-generated ID;
    /// re-uploading the same ID is counted as a duplicate and never creates a second row.
    /// Every scan is validated for tracking-number match, timestamp sanity and status-transition
    /// consistency; failures are returned with a reason code.
    /// </summary>
    public async Task<ScanUploadOutcome> UploadScansAsync(
        string trackingNumber,
        IReadOnlyCollection<ShipmentScan> scans,
        CancellationToken cancellationToken = default)
    {
        var outcome = new ScanUploadOutcome();

        var shipment = await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber, cancellationToken);
        if (shipment == null)
        {
            foreach (var scan in scans)
            {
                outcome.Rejections.Add(new ScanRejection(scan.Id, "shipment_not_found"));
            }
            outcome.Rejected = scans.Count;
            return outcome;
        }

        var currentStatus = shipment.Status;
        foreach (var scan in scans)
        {
            var rejection = await ValidateAndPersistScanAsync(trackingNumber, currentStatus, scan, cancellationToken);
            if (rejection is null)
            {
                outcome.Accepted++;
                if (scan.ScanType == ScanType.Pickup && currentStatus == "Created") currentStatus = "PickedUp";
                else if (scan.ScanType == ScanType.InTransit && currentStatus == "PickedUp") currentStatus = "InTransit";
                else if (scan.ScanType == ScanType.Delivered) currentStatus = "Delivered";
                else if (scan.ScanType == ScanType.Failed) currentStatus = "Failed";
            }
            else if (rejection == DuplicateMarker)
            {
                outcome.DuplicatesSkipped++;
            }
            else
            {
                outcome.Rejected++;
                outcome.Rejections.Add(new ScanRejection(scan.Id, rejection));
            }
        }

        return outcome;
    }

    private const string DuplicateMarker = "__duplicate__";

    private async Task<string?> ValidateAndPersistScanAsync(
        string trackingNumber, string status, ShipmentScan scan, CancellationToken cancellationToken)
    {
        if (!string.Equals(scan.TrackingNumber, trackingNumber, StringComparison.OrdinalIgnoreCase))
        {
            return "tracking_number_mismatch";
        }

        if (scan.ScannedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return "scan_time_in_future";
        }

        if (await _shipmentRepository.GetScanByIdAsync(scan.Id, cancellationToken) is not null)
        {
            return DuplicateMarker;
        }

        // Scan-type / status consistency. Scans are facts recorded offline, so early scan
        // types are accepted while the shipment is still in a compatible state; terminal
        // scans (Delivered/Failed) require the shipment to be OutForDelivery first.
        var (allowed, targetStatus) = scan.ScanType switch
        {
            ScanType.Pickup => (status is "Created" or "PickedUp" or "InTransit" or "OutForDelivery", status == "Created" ? "PickedUp" : null),
            ScanType.InTransit => (status is "PickedUp" or "InTransit" or "OutForDelivery", status == "PickedUp" ? "InTransit" : null),
            ScanType.Delivered => (status == "OutForDelivery", "Delivered"),
            ScanType.Failed => (status == "OutForDelivery", "Failed"),
            _ => (false, null)
        };

        if (!allowed)
        {
            return "invalid_status_transition";
        }

        await _shipmentRepository.AddScanAsync(scan, cancellationToken);

        if (targetStatus is not null)
        {
            await _shipmentRepository.UpdateStatusAsync(trackingNumber, targetStatus, cancellationToken);
        }

        return null;
    }
}

public sealed class ScanUploadOutcome
{
    public int Accepted { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int Rejected { get; set; }
    public List<ScanRejection> Rejections { get; } = new();
}

public sealed record ScanRejection(Guid ScanId, string Reason);
