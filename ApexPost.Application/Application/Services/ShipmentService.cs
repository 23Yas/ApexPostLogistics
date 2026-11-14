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

    /// <summary>Returns the shipment or throws <see cref="NotFoundException"/> (HTTP 404).</summary>
    public async Task<Shipment> GetShipmentAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber, cancellationToken)
            ?? throw new NotFoundException($"No shipment found with tracking number {trackingNumber}");
    }

    public async Task<IReadOnlyCollection<Shipment>> GetPagedAsync(int page, int pageSize, string? status = null, string? city = null, CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new BadRequestException("page must be greater than or equal to 1");
        }
        if (pageSize < 1 || pageSize > 50)
        {
            throw new BadRequestException("pageSize must be between 1 and 50");
        }

        return await _shipmentRepository.GetPagedAsync(page, pageSize, status, city, cancellationToken);
    }

    /// <summary>Creates a shipment; throws <see cref="ConflictException"/> on a duplicate tracking number.</summary>
    public async Task<Shipment> CreateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        var existing = await _shipmentRepository.GetByTrackingNumberAsync(shipment.TrackingReference, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A shipment with tracking number {shipment.TrackingReference} already exists");
        }

        return await _shipmentRepository.CreateAsync(shipment, cancellationToken);
    }

    /// <summary>Updates editable fields; throws <see cref="NotFoundException"/> when the shipment is missing.</summary>
    public async Task<Shipment> UpdateAsync(string trackingNumber, Shipment shipment, CancellationToken cancellationToken = default)
    {
        return await _shipmentRepository.UpdateAsync(trackingNumber, shipment, cancellationToken)
            ?? throw new NotFoundException($"No shipment found with tracking number {trackingNumber}");
    }

    /// <summary>
    /// Applies a status transition and returns the updated shipment.
    /// Throws <see cref="NotFoundException"/> when missing and
    /// <see cref="ConflictException"/> when the transition is not allowed.
    /// </summary>
    public async Task<Shipment> UpdateStatusAsync(string trackingNumber, string newStatus, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(trackingNumber, cancellationToken);
        if (!ShipmentStatusRules.CanTransition(shipment.Status, newStatus))
        {
            throw new ConflictException($"Cannot transition shipment {trackingNumber} from {shipment.Status} to {newStatus}");
        }

        await _shipmentRepository.UpdateStatusAsync(trackingNumber, newStatus, cancellationToken);
        shipment.Status = newStatus;
        return shipment;
    }

    /// <summary>Deletes a shipment; only status Created may be deleted.</summary>
    public async Task DeleteAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(trackingNumber, cancellationToken);
        if (!ShipmentStatusRules.CanDelete(shipment.Status))
        {
            throw new ConflictException(
                $"Shipment {trackingNumber} with status {shipment.Status} cannot be deleted. Only shipments with status 'Created' can be deleted.");
        }

        await _shipmentRepository.DeleteAsync(trackingNumber, cancellationToken);
    }

    /// <summary>Returns scan history; throws <see cref="NotFoundException"/> when the shipment is missing.</summary>
    public async Task<IReadOnlyCollection<ShipmentScan>> GetScansAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        await GetShipmentAsync(trackingNumber, cancellationToken);
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
    /// <exception cref="NotFoundException">The shipment does not exist (HTTP 404).</exception>
    public async Task<ScanUploadOutcome> UploadScansAsync(
        string trackingNumber,
        IReadOnlyCollection<ShipmentScan> scans,
        CancellationToken cancellationToken = default)
    {
        var outcome = new ScanUploadOutcome();

        if (scans.Count > MaxScansPerBatch)
        {
            throw new BadRequestException($"A scan batch may contain at most {MaxScansPerBatch} scans");
        }

        var shipment = await GetShipmentAsync(trackingNumber, cancellationToken);

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

    /// <summary>Maximum scans accepted in a single upload batch.</summary>
    public const int MaxScansPerBatch = 200;

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
