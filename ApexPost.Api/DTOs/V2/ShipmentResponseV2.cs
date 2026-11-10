namespace ApexPost.Api.DTOs.V2;

/// <summary>
/// v2 shipment response with recipient details grouped into an object and the
/// most recent scan included for timeline display.
/// </summary>
public sealed class ShipmentResponseV2
{
    /// <summary>Unique shipment identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Tracking number in the format APX-XXXXXXXX.</summary>
    public string TrackingNumber { get; init; } = string.Empty;

    /// <summary>Parcel weight in kilograms.</summary>
    public decimal WeightKg { get; init; }

    /// <summary>Shipping price in the local currency.</summary>
    public decimal Price { get; init; }

    /// <summary>Current lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Recipient contact and address details.</summary>
    public RecipientDto Recipient { get; init; } = new();

    /// <summary>Most recent scan event, or null when the shipment has no scans yet.</summary>
    public LatestScanDto? LatestScan { get; init; }

    /// <summary>UTC time the shipment was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}

/// <summary>
/// Recipient details grouped for the v2 response contract.
/// </summary>
public sealed class RecipientDto
{
    /// <summary>Full name of the recipient.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Recipient phone number.</summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>Street address of the recipient.</summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>City of the recipient.</summary>
    public string City { get; init; } = string.Empty;
}

/// <summary>
/// Summary of the most recent scan on a shipment.
/// </summary>
public sealed class LatestScanDto
{
    /// <summary>Type of scan event (Pickup, InTransit, Delivered, Failed).</summary>
    public string ScanType { get; init; } = string.Empty;

    /// <summary>Free-text location where the scan was taken.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>UTC time the scan was captured.</summary>
    public DateTimeOffset ScannedAtUtc { get; init; }
}
