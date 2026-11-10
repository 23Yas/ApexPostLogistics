using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApexPost.Api.DTOs;

/// <summary>
/// Request body for bulk upload of offline-collected shipment scans.
/// </summary>
public sealed class ScanUploadRequest
{
    /// <summary>Scans to upload; each carries its client-generated ID for idempotent de-duplication.</summary>
    public List<ShipmentScanDto> Scans { get; init; } = new();
}

/// <summary>
/// Kind of scan event recorded by a courier.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScanType
{
    /// <summary>Parcel collected from the sender.</summary>
    Pickup,
    /// <summary>Parcel moving between facilities.</summary>
    InTransit,
    /// <summary>Parcel handed to the recipient.</summary>
    Delivered,
    /// <summary>Delivery attempt failed.</summary>
    Failed
}

/// <summary>
/// A single offline scan uploaded for a shipment.
/// </summary>
public sealed class ShipmentScanDto : IValidatableObject
{
    /// <summary>Client-generated scan ID; re-uploads of the same ID are de-duplicated.</summary>
    public Guid Id { get; init; }

    /// <summary>Tracking number the scan belongs to; must match the route.</summary>
    [Required]
    public string TrackingNumber { get; init; } = string.Empty;

    /// <summary>Type of scan event.</summary>
    [Required]
    public ScanType ScanType { get; init; }

    /// <summary>Courier who performed the scan; must match the caller's courierId claim.</summary>
    [Required]
    public string CourierId { get; init; } = string.Empty;

    /// <summary>Free-text location where the scan was taken.</summary>
    [Required]
    public string Location { get; init; } = string.Empty;

    /// <summary>UTC time the scan was captured on the device.</summary>
    [Required]
    public DateTimeOffset ScannedAtUtc { get; init; }

    /// <summary>Optional notes; required for Delivered scans (recipient signature).</summary>
    public string Notes { get; init; } = string.Empty;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Custom validation: scan time cannot be in the future
        if (ScannedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            results.Add(new ValidationResult("Scan time cannot be in the future", new[] { nameof(ScannedAtUtc) }));
        }

        // Custom validation: Delivered scan requires notes (recipient signature)
        if (ScanType == ScanType.Delivered && string.IsNullOrWhiteSpace(Notes))
        {
            results.Add(new ValidationResult("Delivered scan requires recipient signature in notes", new[] { nameof(Notes) }));
        }

        return results;
    }
}
